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

// ============ Transactions (§4.5) ============
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ApiControllerBase
{
    private readonly PetroContext _db;
    public TransactionsController(PetroContext db) => _db = db;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var rows = await _db.Transactions.Include(t => t.Commodity)
            .Where(t => t.BuyerId == CurrentUserId || t.SellerId == CurrentUserId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto(t.Id, t.CompetitionId, t.BuyerId, t.SellerId, t.CommodityId,
                t.Commodity.Name, t.Quantity, t.Price, t.Status.ToString(), t.CompletedAt))
            .ToListAsync(ct);
        return Ok(rows);
    }
}

// ============ Surveys & ratings (§4.6) ============
[Route("api/surveys")]
[Authorize]
public class SurveysController : ApiControllerBase
{
    private readonly ISurveyService _svc;
    public SurveysController(ISurveyService svc) => _svc = svc;

    [HttpGet("pending")]
    [HasPermission(Permissions.SurveySubmit)]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var rows = await _svc.GetMyPendingAsync(CurrentUserId, ct);
        return Ok(rows.Select(s => new { s.Id, s.TransactionId, s.RateeId }));
    }

    [HttpPost("{transactionId:int}")]
    [HasPermission(Permissions.SurveySubmit)]
    public async Task<IActionResult> Submit(int transactionId, SubmitSurveyDto dto, CancellationToken ct)
    {
        var s = await _svc.SubmitAsync(CurrentUserId, transactionId,
            new SurveyInput(dto.Score, dto.OnTimeScore, dto.InfoAccuracyScore, dto.Comments), ct);
        return Ok(new { s.Id, s.Completed });
    }
}

[Route("api/ratings")]
public class RatingsController : ApiControllerBase
{
    private readonly PetroContext _db;
    public RatingsController(PetroContext db) => _db = db;

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> Get(int userId, CancellationToken ct)
    {
        var r = await _db.Ratings.FirstOrDefaultAsync(x => x.SubjectUserId == userId, ct);
        return r is null
            ? Ok(new { userId, starScore = 0.0, metrics = "{}" })
            : Ok(new { userId, r.StarScore, r.WeightedScore, metrics = r.MetricsJson, r.UpdatedAt });
    }
}

// ============ Notifications (§4.9) ============
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly PetroContext _db;
    public NotificationsController(PetroContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var q = _db.Notifications.Where(n => n.UserId == CurrentUserId);
        if (unreadOnly) q = q.Where(n => n.ReadAt == null);
        var rows = await q.OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new { n.Id, Channel = n.Channel.ToString(), n.Type, n.Title, n.Payload,
                Status = n.Status.ToString(), n.CreatedAt, n.ReadAt })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> Read(int id, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, ct);
        if (n is null) return NotFound();
        n.ReadAt = DateTime.UtcNow;
        n.Status = NotificationStatus.Read;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPrefs(CancellationToken ct)
    {
        var u = await _db.Users.FirstAsync(x => x.Id == CurrentUserId, ct);
        return Ok(new { u.NotifySms, u.NotifyPush, u.NotifyInApp });
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> SetPrefs([FromBody] NotificationPrefsDto dto, CancellationToken ct)
    {
        var u = await _db.Users.FirstAsync(x => x.Id == CurrentUserId, ct);
        u.NotifySms = dto.Sms;
        u.NotifyPush = dto.Push;
        u.NotifyInApp = dto.InApp;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record NotificationPrefsDto(bool Sms, bool Push, bool InApp);
