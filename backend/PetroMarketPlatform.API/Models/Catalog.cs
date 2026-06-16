using System.ComponentModel.DataAnnotations;

namespace PetroMarketPlatform.API.Models;

/// <summary>A tradeable commodity (petrochemical first; category added per §11 migration table).</summary>
public class Commodity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? Description { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;
}

/// <summary>Seller product listing (Phase 2 marketplace).</summary>
public class Product
{
    public int Id { get; set; }

    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    public string? Specs { get; set; }
    public string? Availability { get; set; }
    public decimal? PriceIndication { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
