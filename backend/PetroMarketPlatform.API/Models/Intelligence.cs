using System.ComponentModel.DataAnnotations;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>News or market-analysis content (§4.2). SEO-friendly via slug.</summary>
public class Article
{
    public int Id { get; set; }

    public ArticleType Type { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? Category { get; set; }

    /// <summary>Comma-separated tags.</summary>
    public string? Tags { get; set; }

    public int? AuthorId { get; set; }
    public User? Author { get; set; }

    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Time-series price point (§4.2 Global Prices).</summary>
public class PriceQuote
{
    public int Id { get; set; }

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    public string? Market { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IRR";
    public DateTime QuotedAt { get; set; }
}

/// <summary>Time-series trade statistic (§4.2 Trade Statistics).</summary>
public class TradeStat
{
    public int Id { get; set; }

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    /// <summary>The period this row aggregates (day/week/month start).</summary>
    public DateTime Period { get; set; }

    public decimal Volume { get; set; }
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
}

/// <summary>Upcoming supply listing (§4.2 Future Supplies).</summary>
public class FutureSupply
{
    public int Id { get; set; }

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    [Required]
    public string Supplier { get; set; } = string.Empty;

    public decimal Volume { get; set; }
    public DateTime SupplyDate { get; set; }
    public string? Location { get; set; }
}

/// <summary>Smart alert subscription on future supplies (§4.2).</summary>
public class SupplyAlert
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Null = all commodities.</summary>
    public int? CommodityId { get; set; }
    public Commodity? Commodity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
