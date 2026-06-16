using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public record CompanyInput(string LegalName, string? RegistrationNumber, string? NationalId, string? Address);

public interface IKycService
{
    Task<KycCase> GetOrCreateCaseAsync(int userId, CancellationToken ct = default);
    Task<KycCase?> GetMyCaseAsync(int userId, CancellationToken ct = default);
    Task<Company> UpsertCompanyAsync(int userId, CompanyInput input, CancellationToken ct = default);
    Task<KycDocument> AddDocumentAsync(int userId, string type, string objectKey,
        string? fileName, string? contentType, CancellationToken ct = default);
    Task SubmitAsync(int userId, CancellationToken ct = default);
    Task<List<KycCase>> GetQueueAsync(CancellationToken ct = default);
    Task ReviewAsync(int reviewerId, int caseId, string decision, string? notes, CancellationToken ct = default);
    Task EnsureCanTradeAsync(int userId, CancellationToken ct = default);
}

public class KycService : IKycService
{
    private readonly PetroContext _db;
    private readonly IAuditService _audit;
    private readonly INotificationService _notify;

    public KycService(PetroContext db, IAuditService audit, INotificationService notify)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
    }

    public async Task<KycCase> GetOrCreateCaseAsync(int userId, CancellationToken ct = default)
    {
        var open = await _db.KycCases
            .Include(k => k.Documents)
            .Where(k => k.UserId == userId &&
                        (k.Status == KycStatus.Draft || k.Status == KycStatus.MoreInfo))
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(ct);
        if (open is not null) return open;

        var c = new KycCase { UserId = userId, Status = KycStatus.Draft };
        _db.KycCases.Add(c);
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        if (user.KycStatus is KycStatus.None) user.KycStatus = KycStatus.Draft;
        await _db.SaveChangesAsync(ct);
        return c;
    }

    public Task<KycCase?> GetMyCaseAsync(int userId, CancellationToken ct = default) =>
        _db.KycCases.Include(k => k.Documents)
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<Company> UpsertCompanyAsync(int userId, CompanyInput input, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Company).FirstAsync(u => u.Id == userId, ct);
        var company = user.Company ?? new Company();
        company.LegalName = input.LegalName;
        company.RegistrationNumber = input.RegistrationNumber;
        company.NationalId = input.NationalId;
        company.Address = input.Address;
        if (company.Id == 0) _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        user.CompanyId = company.Id;
        var kyc = await GetOrCreateCaseAsync(userId, ct);
        kyc.CompanyId = company.Id;
        kyc.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return company;
    }

    public async Task<KycDocument> AddDocumentAsync(int userId, string type, string objectKey,
        string? fileName, string? contentType, CancellationToken ct = default)
    {
        var kyc = await GetOrCreateCaseAsync(userId, ct);
        var doc = new KycDocument
        {
            KycCaseId = kyc.Id,
            Type = type,
            ObjectKey = objectKey,
            FileName = fileName,
            ContentType = contentType
        };
        _db.KycDocuments.Add(doc);
        kyc.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return doc;
    }

    public async Task SubmitAsync(int userId, CancellationToken ct = default)
    {
        var kyc = await _db.KycCases.Include(k => k.Documents)
            .Where(k => k.UserId == userId &&
                        (k.Status == KycStatus.Draft || k.Status == KycStatus.MoreInfo))
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("No draft KYC to submit.");

        if (kyc.CompanyId is null)
            throw new AppException("Company information is required before submitting KYC.");
        if (kyc.Documents.Count == 0)
            throw new AppException("At least one supporting document is required.");

        kyc.Status = KycStatus.Submitted;
        kyc.UpdatedAt = DateTime.UtcNow;
        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        user.KycStatus = KycStatus.Submitted;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("kyc.submit", "KycCase", kyc.Id, actorId: userId, ct: ct);
    }

    public Task<List<KycCase>> GetQueueAsync(CancellationToken ct = default) =>
        _db.KycCases.Include(k => k.User).Include(k => k.Company).Include(k => k.Documents)
            .Where(k => k.Status == KycStatus.Submitted || k.Status == KycStatus.UnderReview)
            .OrderBy(k => k.UpdatedAt)
            .ToListAsync(ct);

    public async Task ReviewAsync(int reviewerId, int caseId, string decision, string? notes, CancellationToken ct = default)
    {
        var kyc = await _db.KycCases.Include(k => k.Company)
            .FirstOrDefaultAsync(k => k.Id == caseId, ct)
            ?? throw AppException.NotFound("KYC case not found.");

        var before = kyc.Status;
        KycStatus next = decision.ToLowerInvariant() switch
        {
            "approve" => KycStatus.Approved,
            "reject" => KycStatus.Rejected,
            "more_info" or "moreinfo" => KycStatus.MoreInfo,
            "review" or "under_review" => KycStatus.UnderReview,
            _ => throw new AppException("Unknown decision. Use approve | reject | more_info | review.")
        };

        if ((next is KycStatus.Rejected or KycStatus.MoreInfo) && string.IsNullOrWhiteSpace(notes))
            throw new AppException("A reason is required when rejecting or requesting more info.");

        kyc.Status = next;
        kyc.ReviewerId = reviewerId;
        kyc.Notes = notes;
        kyc.UpdatedAt = DateTime.UtcNow;
        if (next is KycStatus.Approved or KycStatus.Rejected) kyc.DecidedAt = DateTime.UtcNow;

        var user = await _db.Users.FirstAsync(u => u.Id == kyc.UserId, ct);
        user.KycStatus = next;

        if (next == KycStatus.Approved && kyc.Company is not null)
            kyc.Company.VerifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("kyc.review", "KycCase", kyc.Id,
            before: new { status = before.ToString() },
            after: new { status = next.ToString(), notes }, actorId: reviewerId, ct: ct);

        var msg = next switch
        {
            KycStatus.Approved => "احراز هویت شما تأیید شد. اکنون می‌توانید معامله کنید.",
            KycStatus.Rejected => $"احراز هویت رد شد. دلیل: {notes}",
            KycStatus.MoreInfo => $"اطلاعات بیشتری لازم است: {notes}",
            _ => "وضعیت احراز هویت شما به‌روزرسانی شد."
        };
        await _notify.NotifyAsync(user.Id, "kyc_decision", "نتیجه احراز هویت", msg, sms: true, ct);
    }

    /// <summary>BR-2 hard gate: no trading action proceeds unless KYC is approved.</summary>
    public async Task EnsureCanTradeAsync(int userId, CancellationToken ct = default)
    {
        var status = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.KycStatus).FirstOrDefaultAsync(ct);
        if (status != KycStatus.Approved)
            throw AppException.Forbidden("Trading is blocked until your KYC is approved (BR-2).");
    }
}
