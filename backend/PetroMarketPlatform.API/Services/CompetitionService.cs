using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public record CreateRfqInput(
    int CommodityId, decimal Quantity, string? DeliveryTerms, string? PaymentTerms,
    string? Notes, DateTime Deadline, CompetitionVisibility Visibility = CompetitionVisibility.Confidential);

/// <summary>
/// The RFQ / anonymous reverse-auction engine — the core differentiator (§4.4).
/// Enforces: BR-2 (KYC gate), BR-3 (anonymity), BR-4 (qualified-seller notify), BR-5 (live ranking),
/// BR-6 (top-3 shortlist + reveal), BR-7 (versioned revisions), BR-8 (server-authoritative deadline),
/// BR-9 (feedback gate), and records the resulting Transaction (§4.5).
/// </summary>
public interface ICompetitionService
{
    Task<Competition> CreateRfqAsync(int buyerId, CreateRfqInput input, CancellationToken ct = default);
    Task<Bid> SubmitOrReviseBidAsync(int sellerId, int competitionId, decimal price, decimal quantity, CancellationToken ct = default);
    Task<List<Bid>> GetRankedBidsAsync(int competitionId, CancellationToken ct = default);
    Task<int> CloseDueCompetitionsAsync(CancellationToken ct = default);
    Task<bool> CloseAsync(int competitionId, CancellationToken ct = default);
    Task<List<Shortlist>> ShortlistAsync(int buyerId, int competitionId, IList<int> bidIds, CancellationToken ct = default);
    Task<Transaction> SelectWinnerAsync(int buyerId, int competitionId, int bidId, CancellationToken ct = default);
    Task CancelAsync(int actorId, int competitionId, string reason, CancellationToken ct = default);
}

public class CompetitionService : ICompetitionService
{
    private readonly PetroContext _db;
    private readonly IKycService _kyc;
    private readonly ISurveyService _surveys;
    private readonly INotificationService _notify;
    private readonly IAuditService _audit;
    private readonly ILogger<CompetitionService> _log;

    public CompetitionService(PetroContext db, IKycService kyc, ISurveyService surveys,
        INotificationService notify, IAuditService audit, ILogger<CompetitionService> log)
    {
        _db = db;
        _kyc = kyc;
        _surveys = surveys;
        _notify = notify;
        _audit = audit;
        _log = log;
    }

    public async Task<Competition> CreateRfqAsync(int buyerId, CreateRfqInput input, CancellationToken ct = default)
    {
        await _kyc.EnsureCanTradeAsync(buyerId, ct);          // BR-2
        await _surveys.EnsureNoOutstandingAsync(buyerId, ct); // BR-9

        if (input.Deadline <= DateTime.UtcNow.AddMinutes(1))
            throw new AppException("Deadline must be in the future.");
        if (input.Quantity <= 0)
            throw new AppException("Quantity must be positive.");
        if (!await _db.Commodities.AnyAsync(c => c.Id == input.CommodityId, ct))
            throw AppException.NotFound("Commodity not found.");

        var rfq = new Rfq
        {
            BuyerId = buyerId,
            CommodityId = input.CommodityId,
            Quantity = input.Quantity,
            DeliveryTerms = input.DeliveryTerms,
            PaymentTerms = input.PaymentTerms,
            Notes = input.Notes,
            Deadline = input.Deadline,
            Status = RfqStatus.Open
        };
        _db.Rfqs.Add(rfq);
        await _db.SaveChangesAsync(ct);

        var competition = new Competition
        {
            RfqId = rfq.Id,
            BidWindowStart = DateTime.UtcNow,
            BidWindowEnd = input.Deadline,
            Status = CompetitionStatus.Open,
            Visibility = input.Visibility
        };
        _db.Competitions.Add(competition);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("rfq.create", "Rfq", rfq.Id, after: new { rfq.CommodityId, rfq.Quantity }, actorId: buyerId, ct: ct);
        await NotifyQualifiedSellersAsync(rfq, competition, ct); // BR-4
        return competition;
    }

    /// <summary>
    /// BR-4: notify qualified sellers when an RFQ opens. Default qualification (open question §12.7):
    /// Seller role + KYC approved, restricted to a commodity match when any seller lists that commodity.
    /// </summary>
    private async Task NotifyQualifiedSellersAsync(Rfq rfq, Competition competition, CancellationToken ct)
    {
        var approvedSellerIds = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            join u in _db.Users on ur.UserId equals u.Id
            where r.Name == Auth.Roles.Seller && u.KycStatus == KycStatus.Approved && u.Id != rfq.BuyerId
            select u.Id).Distinct().ToListAsync(ct);

        var commodityMatchSellerIds = await _db.Products
            .Where(p => p.CommodityId == rfq.CommodityId && p.IsActive)
            .Select(p => p.SellerId).Distinct().ToListAsync(ct);

        var targets = commodityMatchSellerIds.Count > 0
            ? approvedSellerIds.Intersect(commodityMatchSellerIds).ToList()
            : approvedSellerIds;

        foreach (var sellerId in targets)
        {
            await _notify.NotifyAsync(sellerId, "rfq_match", "درخواست خرید جدید",
                $"یک مناقصه جدید برای کالای مورد علاقه شما باز شد. مهلت ارسال پیشنهاد: {competition.BidWindowEnd:u}.",
                sms: true, ct);
        }
        _log.LogInformation("RFQ {RfqId}: notified {Count} qualified sellers", rfq.Id, targets.Count);
    }

    public async Task<Bid> SubmitOrReviseBidAsync(int sellerId, int competitionId, decimal price, decimal quantity, CancellationToken ct = default)
    {
        await _kyc.EnsureCanTradeAsync(sellerId, ct);          // BR-2
        await _surveys.EnsureNoOutstandingAsync(sellerId, ct); // BR-9

        if (price <= 0 || quantity <= 0)
            throw new AppException("Price and quantity must be positive.");

        var competition = await _db.Competitions.Include(c => c.Rfq)
            .FirstOrDefaultAsync(c => c.Id == competitionId, ct)
            ?? throw AppException.NotFound("Competition not found.");

        if (competition.Rfq.BuyerId == sellerId)
            throw AppException.Forbidden("You cannot bid on your own RFQ.");

        // BR-8: server-authoritative deadline — no bids after close.
        if (competition.Status != CompetitionStatus.Open || DateTime.UtcNow >= competition.BidWindowEnd)
            throw AppException.Conflict("The bidding window is closed.");

        var bid = await _db.Bids.FirstOrDefaultAsync(b => b.CompetitionId == competitionId && b.SellerId == sellerId, ct);
        if (bid is null)
        {
            bid = new Bid
            {
                CompetitionId = competitionId,
                SellerId = sellerId,
                Price = price,
                Quantity = quantity,
                CurrentVersion = 1
            };
            _db.Bids.Add(bid);
            await _db.SaveChangesAsync(ct);
            _db.BidRevisions.Add(new BidRevision { BidId = bid.Id, Version = 1, Price = price, Quantity = quantity });
        }
        else
        {
            // BR-7: revise until deadline; every revision is versioned and retained.
            bid.CurrentVersion++;
            bid.Price = price;
            bid.Quantity = quantity;
            bid.IsWithdrawn = false;
            bid.UpdatedAt = DateTime.UtcNow;
            _db.BidRevisions.Add(new BidRevision { BidId = bid.Id, Version = bid.CurrentVersion, Price = price, Quantity = quantity });
        }
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("bid.submit", "Bid", bid.Id, after: new { bid.CurrentVersion, price, quantity }, actorId: sellerId, ct: ct);
        return bid;
    }

    /// <summary>BR-5: best offers first (lowest price; earliest submission breaks ties). Live each call.</summary>
    public async Task<List<Bid>> GetRankedBidsAsync(int competitionId, CancellationToken ct = default)
    {
        // Sort client-side: SQLite can't ORDER BY decimal. (PostgreSQL in prod does this server-side.)
        var bids = await _db.Bids
            .Where(b => b.CompetitionId == competitionId && !b.IsWithdrawn)
            .ToListAsync(ct);
        return bids.OrderBy(b => b.Price).ThenBy(b => b.SubmittedAt).ToList();
    }

    /// <summary>
    /// BR-8: close every competition whose window has elapsed. Exactly-once via a guarded update
    /// (only rows still Open transition to Closed). In production this is driven by a delayed job + DB lock.
    /// </summary>
    public async Task<int> CloseDueCompetitionsAsync(CancellationToken ct = default)
    {
        var due = await _db.Competitions
            .Where(c => c.Status == CompetitionStatus.Open && c.BidWindowEnd <= DateTime.UtcNow)
            .ToListAsync(ct);

        var closed = 0;
        foreach (var c in due)
            if (await CloseInternalAsync(c, ct)) closed++;
        return closed;
    }

    public async Task<bool> CloseAsync(int competitionId, CancellationToken ct = default)
    {
        var c = await _db.Competitions.FirstOrDefaultAsync(x => x.Id == competitionId, ct)
            ?? throw AppException.NotFound("Competition not found.");
        return await CloseInternalAsync(c, ct);
    }

    private async Task<bool> CloseInternalAsync(Competition c, CancellationToken ct)
    {
        if (c.Status != CompetitionStatus.Open) return false; // exactly-once guard
        c.Status = CompetitionStatus.Closed;
        c.ClosedAt = DateTime.UtcNow;
        c.Rfq ??= await _db.Rfqs.FirstAsync(r => r.Id == c.RfqId, ct);
        c.Rfq.Status = RfqStatus.Closed;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("competition.close", "Competition", c.Id, ct: ct);
        await _notify.NotifyAsync(c.Rfq.BuyerId, "competition_closed", "پایان مهلت مناقصه",
            "مهلت دریافت پیشنهادها به پایان رسید. اکنون می‌توانید سه پیشنهاد برتر را انتخاب کنید.", ct: ct);
        return true;
    }

    /// <summary>
    /// BR-6: buyer shortlists up to 3 offers; only then are seller identities revealed (audited, BR-3).
    /// </summary>
    public async Task<List<Shortlist>> ShortlistAsync(int buyerId, int competitionId, IList<int> bidIds, CancellationToken ct = default)
    {
        var competition = await _db.Competitions.Include(c => c.Rfq)
            .FirstOrDefaultAsync(c => c.Id == competitionId, ct)
            ?? throw AppException.NotFound("Competition not found.");

        if (competition.Rfq.BuyerId != buyerId)
            throw AppException.Forbidden("Only the RFQ owner can shortlist.");
        if (competition.Status == CompetitionStatus.Open)
            throw AppException.Conflict("Wait until the bidding window closes before shortlisting.");
        if (bidIds.Count is 0 or > 3)
            throw new AppException("Shortlist between 1 and 3 offers (BR-6).");

        var bids = await _db.Bids
            .Where(b => bidIds.Contains(b.Id) && b.CompetitionId == competitionId && !b.IsWithdrawn)
            .ToListAsync(ct);
        if (bids.Count != bidIds.Count)
            throw new AppException("One or more selected offers are invalid for this competition.");

        // Replace any existing shortlist.
        var existing = _db.Shortlists.Where(s => s.CompetitionId == competitionId);
        _db.Shortlists.RemoveRange(existing);

        var ranked = bids.OrderBy(b => b.Price).ThenBy(b => b.SubmittedAt).ToList();
        var rows = new List<Shortlist>();
        for (var i = 0; i < ranked.Count; i++)
        {
            var s = new Shortlist { CompetitionId = competitionId, BidId = ranked[i].Id, Rank = i + 1 };
            rows.Add(s);
            _db.Shortlists.Add(s);
        }

        competition.IdentitiesRevealed = true; // reveal stage (BR-3 / BR-6)
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("competition.reveal", "Competition", competition.Id,
            after: new { shortlistedBidIds = ranked.Select(b => b.Id), revealedSellerIds = ranked.Select(b => b.SellerId) },
            actorId: buyerId, ct: ct);

        return rows;
    }

    public async Task<Transaction> SelectWinnerAsync(int buyerId, int competitionId, int bidId, CancellationToken ct = default)
    {
        var competition = await _db.Competitions.Include(c => c.Rfq)
            .FirstOrDefaultAsync(c => c.Id == competitionId, ct)
            ?? throw AppException.NotFound("Competition not found.");

        if (competition.Rfq.BuyerId != buyerId)
            throw AppException.Forbidden("Only the RFQ owner can select the winner.");
        if (competition.Status == CompetitionStatus.Awarded)
            throw AppException.Conflict("A winner has already been selected.");
        if (competition.Status != CompetitionStatus.Closed)
            throw AppException.Conflict("Close the competition and shortlist before selecting a winner.");

        var inShortlist = await _db.Shortlists.AnyAsync(s => s.CompetitionId == competitionId && s.BidId == bidId, ct);
        if (!inShortlist)
            throw new AppException("The winner must be chosen from the shortlist (BR-6).");

        var bid = await _db.Bids.FirstAsync(b => b.Id == bidId, ct);

        competition.WinningBidId = bid.Id;
        competition.Status = CompetitionStatus.Awarded;
        competition.AwardedAt = DateTime.UtcNow;
        competition.Rfq.Status = RfqStatus.Awarded;

        var txn = new Transaction
        {
            CompetitionId = competition.Id,
            BuyerId = buyerId,
            SellerId = bid.SellerId,
            CommodityId = competition.Rfq.CommodityId,
            Quantity = bid.Quantity,
            Price = bid.Price,
            Status = TransactionStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(txn);
        await _db.SaveChangesAsync(ct);

        // Mandatory post-trade surveys for both parties (BR-9): create the pending pair.
        _db.Surveys.Add(new Survey { TransactionId = txn.Id, RaterId = buyerId, RateeId = bid.SellerId });
        _db.Surveys.Add(new Survey { TransactionId = txn.Id, RaterId = bid.SellerId, RateeId = buyerId });
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("competition.award", "Competition", competition.Id,
            after: new { winningBidId = bid.Id, txnId = txn.Id }, actorId: buyerId, ct: ct);

        await _notify.NotifyAsync(bid.SellerId, "winner", "برنده مناقصه",
            "پیشنهاد شما به عنوان برنده انتخاب شد. لطفاً پس از تکمیل معامله نظرسنجی را پر کنید.", sms: true, ct);
        await _notify.NotifyAsync(buyerId, "winner", "ثبت معامله",
            "برنده انتخاب شد و معامله ثبت گردید. لطفاً نظرسنجی پس از معامله را تکمیل کنید.", ct: ct);

        return txn;
    }

    public async Task CancelAsync(int actorId, int competitionId, string reason, CancellationToken ct = default)
    {
        var competition = await _db.Competitions.Include(c => c.Rfq)
            .FirstOrDefaultAsync(c => c.Id == competitionId, ct)
            ?? throw AppException.NotFound("Competition not found.");
        if (competition.Status == CompetitionStatus.Awarded)
            throw AppException.Conflict("Cannot cancel an awarded competition.");

        competition.Status = CompetitionStatus.Cancelled;
        competition.Rfq.Status = RfqStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("competition.cancel", "Competition", competition.Id, after: new { reason }, actorId: actorId, ct: ct);
    }
}
