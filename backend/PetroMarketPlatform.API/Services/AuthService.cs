using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public record OtpRequestResult(bool Sent, int TtlSeconds, string? DevCode);

public interface IAuthService
{
    Task<OtpRequestResult> RequestOtpAsync(string mobile, OtpPurpose purpose, CancellationToken ct = default);
    Task<AuthTokens> VerifyAsync(string mobile, string code, OtpPurpose purpose,
        string? fullName, IEnumerable<string>? roles, CancellationToken ct = default);
}

/// <summary>Mobile + OTP auth (BR-1). OTP is rate-limited, expiring, and attempt-limited (§7).</summary>
public class AuthService : IAuthService
{
    private readonly PetroContext _db;
    private readonly ISmsSender _sms;
    private readonly IAuditService _audit;
    private readonly OtpOptions _otp;
    private readonly ITokenService _tokens;
    private readonly IHostEnvironment _env;

    public AuthService(PetroContext db, ISmsSender sms, IAuditService audit,
        IOptions<OtpOptions> otp, ITokenService tokens, IHostEnvironment env)
    {
        _db = db;
        _sms = sms;
        _audit = audit;
        _otp = otp.Value;
        _tokens = tokens;
        _env = env;
    }

    public async Task<OtpRequestResult> RequestOtpAsync(string mobile, OtpPurpose purpose, CancellationToken ct = default)
    {
        mobile = Normalize(mobile);

        var since = DateTime.UtcNow.AddHours(-1);
        var recent = await _db.OtpRequests.CountAsync(o => o.Mobile == mobile && o.CreatedAt >= since, ct);
        if (recent >= _otp.MaxRequestsPerHour)
            throw new AppException("Too many OTP requests. Please try again later.", 429, "rate_limited");

        if (purpose == OtpPurpose.Login && !await _db.Users.AnyAsync(u => u.Mobile == mobile, ct))
            throw AppException.NotFound("No account for this mobile. Please register first.");

        var code = Hashing.NumericCode(_otp.Length);
        _db.OtpRequests.Add(new OtpRequest
        {
            Mobile = mobile,
            CodeHash = Hashing.Sha256(code),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_otp.TtlSeconds)
        });
        await _db.SaveChangesAsync(ct);

        await _sms.SendAsync(mobile, $"کد ورود پتکی: {code} (اعتبار {_otp.TtlSeconds} ثانیه)", ct);

        // Dev convenience only: never expose the code outside a Development environment, regardless of config.
        var devCode = _otp.DevExposeCode && _env.IsDevelopment() ? code : null;
        return new OtpRequestResult(true, _otp.TtlSeconds, devCode);
    }

    public async Task<AuthTokens> VerifyAsync(string mobile, string code, OtpPurpose purpose,
        string? fullName, IEnumerable<string>? roles, CancellationToken ct = default)
    {
        mobile = Normalize(mobile);

        var otp = await _db.OtpRequests
            .Where(o => o.Mobile == mobile && o.Purpose == purpose && !o.Consumed)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("No active OTP. Request a new code.", 400, "otp_missing");

        if (otp.ExpiresAt < DateTime.UtcNow)
            throw new AppException("OTP expired. Request a new code.", 400, "otp_expired");

        if (otp.AttemptCount >= _otp.MaxVerifyAttempts)
            throw new AppException("Too many attempts. Request a new code.", 429, "otp_locked");

        otp.AttemptCount++;
        if (otp.CodeHash != Hashing.Sha256(code))
        {
            await _db.SaveChangesAsync(ct);
            throw new AppException("Incorrect code.", 400, "otp_invalid");
        }

        otp.Consumed = true;
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == mobile, ct);

        if (purpose == OtpPurpose.Register)
        {
            if (user is not null)
                throw AppException.Conflict("An account already exists for this mobile. Please log in.");
            user = await CreateUserAsync(mobile, fullName, roles, ct);
            await _audit.LogAsync("user.register", "User", user.Id, after: new { mobile }, actorId: user.Id, ct: ct);
        }
        else
        {
            if (user is null) throw AppException.NotFound("Account not found.");
            await _audit.LogAsync("user.login", "User", user.Id, actorId: user.Id, ct: ct);
        }

        return await _tokens.IssueAsync(user, ct);
    }

    private async Task<User> CreateUserAsync(string mobile, string? fullName,
        IEnumerable<string>? requestedRoles, CancellationToken ct)
    {
        var user = new User { Mobile = mobile, FullName = fullName, KycStatus = KycStatus.None };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Default to Buyer intent; a user may also pick Seller. (Trading still gated by KYC, BR-2.)
        var wanted = (requestedRoles ?? new[] { Roles.Buyer })
            .Select(r => r.Trim())
            .Where(r => r == Roles.Buyer || r == Roles.Seller)
            .Distinct()
            .ToList();
        if (wanted.Count == 0) wanted.Add(Roles.Buyer);

        var roleRows = await _db.Roles.Where(r => wanted.Contains(r.Name)).ToListAsync(ct);
        foreach (var r in roleRows)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = r.Id });

        // Seed a neutral starting rating.
        _db.Ratings.Add(new Rating { SubjectUserId = user.Id, StarScore = 0, WeightedScore = 0, MetricsJson = "{}" });
        await _db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>Normalize Iranian mobile forms (+98 / 0098 / 9xxxxxxxxx) to 0XXXXXXXXXX.</summary>
    private static string Normalize(string mobile)
    {
        var m = new string(mobile.Where(char.IsDigit).ToArray());
        if (m.StartsWith("0098")) m = m[4..];
        else if (m.StartsWith("98") && m.Length == 12) m = m[2..];
        if (m.Length == 10 && m.StartsWith("9")) m = "0" + m;
        return m;
    }
}
