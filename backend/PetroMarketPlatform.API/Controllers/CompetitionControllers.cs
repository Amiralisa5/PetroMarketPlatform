using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Dtos;
using PetroMarketPlatform.API.Models;
using PetroMarketPlatform.API.Services;

namespace PetroMarketPlatform.API.Controllers;

// ============ RFQ (buyer) ============
[Route("api/rfqs")]
public class RfqsController : ApiControllerBase
{
    private readonly PetroContext _db;
    private readonly ICompetitionService _svc;

    public RfqsController(PetroContext db, ICompetitionService svc)
    {
        _db = db;
        _svc = svc;
    }

    [HttpPost]
    [HasPermission(Permissions.RfqManage)]
    public async Task<IActionResult> Create(CreateRfqDto dto, CancellationToken ct)
    {
        var competition = await _svc.CreateRfqAsync(CurrentUserId,
            new CreateRfqInput(dto.CommodityId, dto.Quantity, dto.DeliveryTerms, dto.PaymentTerms, dto.Notes, dto.Deadline, dto.Visibility), ct);
        return Ok(new { competitionId = competition.Id, rfqId = competition.RfqId });
    }

    [HttpGet("mine")]
    [HasPermission(Permissions.RfqManage)]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var rows = await _db.Rfqs.Include(r => r.Commodity).Include(r => r.Competition)
            .Where(r => r.BuyerId == CurrentUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.CommodityId, Commodity = r.Commodity.Name, r.Quantity, r.Deadline,
                Status = r.Status.ToString(), CompetitionId = r.Competition!.Id,
                CompetitionStatus = r.Competition.Status.ToString()
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var r = await _db.Rfqs.Include(x => x.Commodity).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();

        // BR-3 anonymity: the buyer's identity is hidden from sellers / the public.
        // Only the RFQ owner and monitors (operator/admin) see the real BuyerId; others get 0.
        var viewerId = CurrentUserIdOrNull;
        var privileged = HasPerm(Permissions.CompetitionMonitor) || (viewerId is not null && r.BuyerId == viewerId);
        var buyerId = privileged ? r.BuyerId : 0;

        return Ok(new RfqDto(r.Id, buyerId, r.CommodityId, r.Commodity.Name,
            r.Quantity, r.DeliveryTerms, r.PaymentTerms, r.Notes, r.Deadline, r.Status.ToString(), r.CreatedAt));
    }
}

// ============ Competition / anonymous bidding (core) ============
[Route("api/competitions")]
public class CompetitionsController : ApiControllerBase
{
    private readonly PetroContext _db;
    private readonly ICompetitionService _svc;

    public CompetitionsController(PetroContext db, ICompetitionService svc)
    {
        _db = db;
        _svc = svc;
    }

    /// <summary>Public list of open competitions — confidential-safe (no prices, BR-11).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await _db.Competitions.Include(c => c.Rfq).ThenInclude(r => r.Commodity)
            .Where(c => c.Status == CompetitionStatus.Open)
            .OrderBy(c => c.BidWindowEnd)
            .Select(c => new
            {
                c.Id, c.RfqId, Commodity = c.Rfq.Commodity.Name, c.Rfq.Quantity,
                c.BidWindowEnd, Status = c.Status.ToString(), BidCount = c.Bids.Count
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompetitionDto>> Get(int id, CancellationToken ct)
    {
        var c = await _db.Competitions.Include(x => x.Rfq).ThenInclude(r => r.Commodity)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        var bidCount = await _db.Bids.CountAsync(b => b.CompetitionId == id && !b.IsWithdrawn, ct);
        var rfq = c.Rfq;

        // BR-3 anonymity: the buyer's identity is hidden from sellers / the public until reveal.
        // Only the RFQ owner and monitors (operator/admin) see the real BuyerId; others get 0.
        var viewerId = CurrentUserIdOrNull;
        var privileged = HasPerm(Permissions.CompetitionMonitor) || (viewerId is not null && rfq.BuyerId == viewerId);
        var buyerId = privileged ? rfq.BuyerId : 0;

        return Ok(new CompetitionDto(c.Id, c.RfqId, rfq.Commodity.Name, rfq.Quantity, c.BidWindowStart,
            c.BidWindowEnd, c.Status.ToString(), c.Visibility.ToString(), c.IdentitiesRevealed, bidCount,
            c.WinningBidId,
            new RfqDto(rfq.Id, buyerId, rfq.CommodityId, rfq.Commodity.Name, rfq.Quantity,
                rfq.DeliveryTerms, rfq.PaymentTerms, rfq.Notes, rfq.Deadline, rfq.Status.ToString(), rfq.CreatedAt)));
    }

    /// <summary>
    /// Ranked offers (BR-5), with anonymity enforced at the serialization layer (BR-3).
    /// Seller identities are returned only to the RFQ owner after reveal, or to monitors (operator/admin).
    /// </summary>
    [HttpGet("{id:int}/bids")]
    public async Task<IActionResult> Bids(int id, CancellationToken ct)
    {
        var c = await _db.Competitions.Include(x => x.Rfq).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();

        var ranked = await _svc.GetRankedBidsAsync(id, ct);
        var shortlistedIds = (await _db.Shortlists.Where(s => s.CompetitionId == id)
            .Select(s => s.BidId).ToListAsync(ct)).ToHashSet();

        var viewerId = CurrentUserIdOrNull;
        var isOwner = viewerId is not null && c.Rfq.BuyerId == viewerId;
        var isMonitor = HasPerm(Permissions.CompetitionMonitor);
        var canSeeIdentities = isMonitor || (isOwner && c.IdentitiesRevealed);

        // BR-3 / BR-11: identities revealed only to owner-after-reveal or monitors.
        if (canSeeIdentities)
        {
            var sellerIds = ranked.Select(b => b.SellerId).Distinct().ToList();
            var sellers = await _db.Users.Include(u => u.Company).Include(u => u.Rating)
                .Where(u => sellerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

            var revealed = ranked.Select((b, i) =>
            {
                sellers.TryGetValue(b.SellerId, out var s);
                return new BidRevealedDto(b.Id, i + 1, b.Price, b.Quantity, b.CurrentVersion, b.SubmittedAt,
                    b.SellerId, s?.FullName, s?.Company?.LegalName, s?.Rating?.StarScore ?? 0,
                    shortlistedIds.Contains(b.Id));
            });
            return Ok(revealed);
        }

        // BR-11 price confidentiality: the full ranked price ladder is exposed (anonymized) only to the
        // RFQ owner (pre-reveal), monitors, or anyone when the competition is explicitly Public.
        var canSeeLadder = isMonitor || isOwner || c.Visibility == CompetitionVisibility.Public;
        if (canSeeLadder)
        {
            var pub = ranked.Select((b, i) => new BidPublicDto(b.Id, i + 1, b.Price, b.Quantity,
                b.CurrentVersion, b.SubmittedAt, viewerId is not null && b.SellerId == viewerId));
            return Ok(pub);
        }

        // Confidential / AggregateOnly + non-owner/non-monitor: never leak rivals' prices.
        // A participating seller may see only their OWN bid (with its true rank).
        if (viewerId is not null)
        {
            var idx = ranked.FindIndex(b => b.SellerId == viewerId);
            if (idx >= 0)
            {
                var b = ranked[idx];
                return Ok(new[] { new BidPublicDto(b.Id, idx + 1, b.Price, b.Quantity, b.CurrentVersion, b.SubmittedAt, true) });
            }
        }
        return Ok(new { confidential = true, bidCount = ranked.Count });
    }

    [HttpPost("{id:int}/bids")]
    [HasPermission(Permissions.BidSubmit)]
    public async Task<IActionResult> SubmitBid(int id, SubmitBidDto dto, CancellationToken ct)
    {
        var bid = await _svc.SubmitOrReviseBidAsync(CurrentUserId, id, dto.Price, dto.Quantity, ct);
        return Ok(new { bid.Id, bid.CurrentVersion });
    }

    [HttpGet("{id:int}/bids/{bidId:int}/revisions")]
    [Authorize]
    public async Task<IActionResult> Revisions(int id, int bidId, CancellationToken ct)
    {
        var bid = await _db.Bids.Include(b => b.Competition).ThenInclude(c => c.Rfq)
            .FirstOrDefaultAsync(b => b.Id == bidId && b.CompetitionId == id, ct);
        if (bid is null) return NotFound();

        var allowed = bid.SellerId == CurrentUserId || bid.Competition.Rfq.BuyerId == CurrentUserId || HasPerm(Permissions.CompetitionMonitor);
        if (!allowed) throw AppException.Forbidden("You cannot view this bid's history.");

        var revs = await _db.BidRevisions.Where(r => r.BidId == bidId).OrderBy(r => r.Version)
            .Select(r => new { r.Version, r.Price, r.Quantity, r.CreatedAt }).ToListAsync(ct);
        return Ok(revs);
    }

    [HttpPost("{id:int}/shortlist")]
    [HasPermission(Permissions.CompetitionShortlist)]
    public async Task<IActionResult> Shortlist(int id, ShortlistDto dto, CancellationToken ct)
    {
        var rows = await _svc.ShortlistAsync(CurrentUserId, id, dto.BidIds, ct);
        return Ok(rows.Select(r => new { r.BidId, r.Rank }));
    }

    [HttpPost("{id:int}/winner")]
    [HasPermission(Permissions.CompetitionShortlist)]
    public async Task<IActionResult> SelectWinner(int id, SelectWinnerDto dto, CancellationToken ct)
    {
        var txn = await _svc.SelectWinnerAsync(CurrentUserId, id, dto.BidId, ct);
        return Ok(new { transactionId = txn.Id, txn.SellerId, txn.Price, txn.Quantity });
    }

    /// <summary>Operator/Admin can close a competition early (intervention/monitoring, §4.8).</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(Permissions.CompetitionMonitor)]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var ok = await _svc.CloseAsync(id, ct);
        return Ok(new { closed = ok });
    }

    /// <summary>Admin dispute intervention: cancel a competition (§4.8).</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.CompetitionIntervene)]
    public async Task<IActionResult> Cancel(int id, [FromBody] ReasonDto dto, CancellationToken ct)
    {
        await _svc.CancelAsync(CurrentUserId, id, dto.Reason, ct);
        return NoContent();
    }
}

public record ReasonDto(string Reason);
