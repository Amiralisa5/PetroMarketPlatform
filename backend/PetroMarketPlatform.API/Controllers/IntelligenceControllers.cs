using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Dtos;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Controllers;

// ============ Commodities (reference data) ============
public class CommoditiesController : ApiControllerBase
{
    private readonly PetroContext _db;
    public CommoditiesController(PetroContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.Commodities
            .Select(c => new CommodityDto(c.Id, c.Name, c.Category, c.Description, c.Unit))
            .ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var c = await _db.Commodities.FindAsync(new object?[] { id }, ct);
        return c is null ? NotFound() : Ok(new CommodityDto(c.Id, c.Name, c.Category, c.Description, c.Unit));
    }

    [HttpPost]
    [HasPermission(Permissions.ContentManage)]
    public async Task<IActionResult> Create(Commodity c, CancellationToken ct)
    {
        _db.Commodities.Add(c);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c);
    }
}

// ============ News & Market Analysis (§4.2) ============
[Route("api/articles")]
public class ArticlesController : ApiControllerBase
{
    private readonly PetroContext _db;
    public ArticlesController(PetroContext db) => _db = db;

    /// <summary>Public list. Filter by type (news|analysis), tag, or free-text search.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type, [FromQuery] string? tag,
        [FromQuery] string? q, CancellationToken ct = default)
    {
        var query = _db.Articles.Where(a => a.IsPublished);
        if (Enum.TryParse<ArticleType>(type, true, out var t)) query = query.Where(a => a.Type == t);
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(a => a.Tags != null && a.Tags.Contains(tag));
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => a.Title.Contains(q) || (a.Summary != null && a.Summary.Contains(q)));
        var items = await query.OrderByDescending(a => a.PublishedAt)
            .Select(a => new { a.Id, a.Type, a.Title, a.Slug, a.Summary, a.Category, a.Tags, a.PublishedAt })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken ct)
    {
        var a = await _db.Articles.FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished, ct);
        return a is null ? NotFound() : Ok(a);
    }

    [HttpPost]
    [HasPermission(Permissions.ContentManage)]
    public async Task<IActionResult> Create(CreateArticleDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<ArticleType>(dto.Type, true, out var t))
            throw new AppException("type must be 'news' or 'analysis'.");
        if (await _db.Articles.AnyAsync(a => a.Slug == dto.Slug, ct))
            throw AppException.Conflict("Slug already exists.");
        var a = new Article
        {
            Type = t, Title = dto.Title, Slug = dto.Slug, Summary = dto.Summary, Body = dto.Body,
            Category = dto.Category, Tags = dto.Tags, AuthorId = CurrentUserId, IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null
        };
        _db.Articles.Add(a);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { slug = a.Slug }, a);
    }
}

// ============ Global Prices (§4.2) ============
[Route("api/prices")]
public class PricesController : ApiControllerBase
{
    private readonly PetroContext _db;
    public PricesController(PetroContext db) => _db = db;

    /// <summary>Current price per commodity (latest quote).</summary>
    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken ct)
    {
        // Pick the latest quote per commodity in memory: EF Core can't translate
        // "select the whole row per group via First()" and throws at runtime (SQLite/SqlServer).
        var quotes = await _db.PriceQuotes
            .Select(p => new { p.CommodityId, p.Price, p.Currency, p.Market, p.QuotedAt })
            .ToListAsync(ct);

        var latest = quotes
            .GroupBy(p => p.CommodityId)
            .Select(g => g.OrderByDescending(p => p.QuotedAt).First());

        var commodities = await _db.Commodities.ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        return Ok(latest.Select(p => new
        {
            p.CommodityId, Commodity = commodities.GetValueOrDefault(p.CommodityId),
            p.Price, p.Currency, p.Market, p.QuotedAt
        }));
    }

    /// <summary>Historical time-series for a commodity (for trend charts).</summary>
    [HttpGet("history/{commodityId:int}")]
    public async Task<IActionResult> History(int commodityId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var series = await _db.PriceQuotes
            .Where(p => p.CommodityId == commodityId && p.QuotedAt >= since)
            .OrderBy(p => p.QuotedAt)
            .Select(p => new { p.QuotedAt, p.Price, p.Currency })
            .ToListAsync(ct);
        return Ok(series);
    }

    [HttpPost]
    [HasPermission(Permissions.ContentManage)]
    public async Task<IActionResult> Create(CreatePriceQuoteDto dto, CancellationToken ct)
    {
        var p = new PriceQuote { CommodityId = dto.CommodityId, Market = dto.Market, Price = dto.Price, Currency = dto.Currency, QuotedAt = dto.QuotedAt };
        _db.PriceQuotes.Add(p);
        await _db.SaveChangesAsync(ct);
        return Ok(p);
    }
}

// ============ Trade Statistics (§4.2) ============
[Route("api/trade-stats")]
public class TradeStatsController : ApiControllerBase
{
    private readonly PetroContext _db;
    public TradeStatsController(PetroContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? commodityId, CancellationToken ct = default)
    {
        var q = _db.TradeStats.AsQueryable();
        if (commodityId is not null) q = q.Where(t => t.CommodityId == commodityId);
        var rows = await q.OrderBy(t => t.Period)
            .Select(t => new { t.CommodityId, t.Period, t.Volume, t.BasePrice, t.FinalPrice })
            .ToListAsync(ct);
        return Ok(rows);
    }

    /// <summary>Aggregated dashboard totals.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var rows = await _db.TradeStats.ToListAsync(ct);
        return Ok(new
        {
            totalVolume = rows.Sum(r => r.Volume),
            avgBasePrice = rows.Count > 0 ? rows.Average(r => r.BasePrice) : 0,
            avgFinalPrice = rows.Count > 0 ? rows.Average(r => r.FinalPrice) : 0,
            byCommodity = rows.GroupBy(r => r.CommodityId)
                .Select(g => new { commodityId = g.Key, volume = g.Sum(x => x.Volume) })
        });
    }

    [HttpPost]
    [HasPermission(Permissions.ContentManage)]
    public async Task<IActionResult> Create(CreateTradeStatDto dto, CancellationToken ct)
    {
        var t = new TradeStat { CommodityId = dto.CommodityId, Period = dto.Period, Volume = dto.Volume, BasePrice = dto.BasePrice, FinalPrice = dto.FinalPrice };
        _db.TradeStats.Add(t);
        await _db.SaveChangesAsync(ct);
        return Ok(t);
    }
}

// ============ Future Supplies (§4.2) ============
[Route("api/future-supplies")]
public class FutureSuppliesController : ApiControllerBase
{
    private readonly PetroContext _db;
    public FutureSuppliesController(PetroContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? commodityId, CancellationToken ct = default)
    {
        var q = _db.FutureSupplies.Include(f => f.Commodity).AsQueryable();
        if (commodityId is not null) q = q.Where(f => f.CommodityId == commodityId);
        var rows = await q.OrderBy(f => f.SupplyDate)
            .Select(f => new { f.Id, f.CommodityId, Commodity = f.Commodity.Name, f.Supplier, f.Volume, f.SupplyDate, f.Location })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    [HasPermission(Permissions.ContentManage)]
    public async Task<IActionResult> Create(CreateFutureSupplyDto dto, CancellationToken ct)
    {
        var f = new FutureSupply { CommodityId = dto.CommodityId, Supplier = dto.Supplier, Volume = dto.Volume, SupplyDate = dto.SupplyDate, Location = dto.Location };
        _db.FutureSupplies.Add(f);
        await _db.SaveChangesAsync(ct);
        return Ok(f);
    }

    /// <summary>Subscribe to smart alerts for a commodity (null = all).</summary>
    [HttpPost("alerts")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Subscribe([FromQuery] int? commodityId, CancellationToken ct)
    {
        var exists = await _db.SupplyAlerts.AnyAsync(a => a.UserId == CurrentUserId && a.CommodityId == commodityId, ct);
        if (!exists)
        {
            _db.SupplyAlerts.Add(new SupplyAlert { UserId = CurrentUserId, CommodityId = commodityId });
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    [HttpGet("alerts")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> MyAlerts(CancellationToken ct) =>
        Ok(await _db.SupplyAlerts.Where(a => a.UserId == CurrentUserId)
            .Select(a => new { a.Id, a.CommodityId, a.CreatedAt }).ToListAsync(ct));
}
