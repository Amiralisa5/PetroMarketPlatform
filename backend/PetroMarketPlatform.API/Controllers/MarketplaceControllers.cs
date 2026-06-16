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

// ============ Products / catalog (§4.3) ============
[Route("api/products")]
public class ProductsController : ApiControllerBase
{
    private readonly PetroContext _db;
    private readonly IKycService _kyc;

    public ProductsController(PetroContext db, IKycService kyc)
    {
        _db = db;
        _kyc = kyc;
    }

    /// <summary>Public catalog browse/search/filter by commodity, seller or location-ish text.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? commodityId, [FromQuery] int? sellerId,
        [FromQuery] string? q, CancellationToken ct = default)
    {
        var query = _db.Products.Include(p => p.Seller).Include(p => p.Commodity).Where(p => p.IsActive);
        if (commodityId is not null) query = query.Where(p => p.CommodityId == commodityId);
        if (sellerId is not null) query = query.Where(p => p.SellerId == sellerId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Specs!.Contains(q) || p.Commodity.Name.Contains(q));

        var items = await query.Select(p => new ProductDto(
            p.Id, p.SellerId, p.Seller.FullName ?? "Seller", p.CommodityId, p.Commodity.Name,
            p.Specs, p.Availability, p.PriceIndication, p.IsActive)).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("mine")]
    [HasPermission(Permissions.ProductManage)]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var items = await _db.Products.Include(p => p.Commodity).Where(p => p.SellerId == CurrentUserId)
            .Select(p => new ProductDto(p.Id, p.SellerId, "me", p.CommodityId, p.Commodity.Name,
                p.Specs, p.Availability, p.PriceIndication, p.IsActive)).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [HasPermission(Permissions.ProductManage)]
    public async Task<IActionResult> Create(CreateProductDto dto, CancellationToken ct)
    {
        await _kyc.EnsureCanTradeAsync(CurrentUserId, ct); // sellers must be KYC-approved (BR-2)
        if (!await _db.Commodities.AnyAsync(c => c.Id == dto.CommodityId, ct))
            throw AppException.NotFound("Commodity not found.");
        var p = new Product
        {
            SellerId = CurrentUserId, CommodityId = dto.CommodityId, Specs = dto.Specs,
            Availability = dto.Availability, PriceIndication = dto.PriceIndication
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return Ok(new { p.Id });
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ProductManage)]
    public async Task<IActionResult> Update(int id, CreateProductDto dto, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.SellerId == CurrentUserId, ct)
            ?? throw AppException.NotFound("Product not found.");
        p.CommodityId = dto.CommodityId;
        p.Specs = dto.Specs;
        p.Availability = dto.Availability;
        p.PriceIndication = dto.PriceIndication;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ProductManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.SellerId == CurrentUserId, ct)
            ?? throw AppException.NotFound("Product not found.");
        p.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

// ============ Public seller profiles (§4.3) ============
[Route("api/sellers")]
public class SellersController : ApiControllerBase
{
    private readonly PetroContext _db;
    public SellersController(PetroContext db) => _db = db;

    [HttpGet("{sellerId:int}")]
    public async Task<IActionResult> Profile(int sellerId, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.Company).Include(u => u.Rating)
            .FirstOrDefaultAsync(u => u.Id == sellerId, ct);
        if (user is null) return NotFound();

        var products = await _db.Products.Include(p => p.Commodity)
            .Where(p => p.SellerId == sellerId && p.IsActive)
            .Select(p => new ProductDto(p.Id, p.SellerId, user.FullName ?? "Seller", p.CommodityId,
                p.Commodity.Name, p.Specs, p.Availability, p.PriceIndication, p.IsActive))
            .ToListAsync(ct);

        return Ok(new SellerProfileDto(sellerId, user.FullName, user.Company?.LegalName,
            user.Rating?.StarScore ?? 0, user.Rating?.MetricsJson ?? "{}", products));
    }
}
