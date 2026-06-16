using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>Recorded outcome of a won competition (§4.5). Settlement itself is off-platform by default.</summary>
public class Transaction
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}

/// <summary>
/// Mandatory post-trade feedback from both parties (§4.6). One per (transaction, rater).
/// Outstanding surveys block future transactions (BR-9).
/// </summary>
public class Survey
{
    public int Id { get; set; }

    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public int RaterId { get; set; }
    public User Rater { get; set; } = null!;

    public int RateeId { get; set; }
    public User Ratee { get; set; } = null!;

    /// <summary>Overall 1–5.</summary>
    public int Score { get; set; }
    /// <summary>On-time fulfillment 1–5 (feeds BR-10).</summary>
    public int OnTimeScore { get; set; }
    /// <summary>Information accuracy 1–5 (feeds BR-10).</summary>
    public int InfoAccuracyScore { get; set; }

    public string? Comments { get; set; }

    public bool Completed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Weighted reputation per user (BR-10). Recomputed on each new transaction/survey.</summary>
public class Rating
{
    public int Id { get; set; }

    public int SubjectUserId { get; set; }
    public User SubjectUser { get; set; } = null!;

    /// <summary>1–5 star display.</summary>
    public double StarScore { get; set; }

    /// <summary>Raw weighted score before normalization.</summary>
    public double WeightedScore { get; set; }

    /// <summary>JSON of contributing metrics (survey avg, txn count, on-time, info accuracy, complaints).</summary>
    public string MetricsJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
