using System.ComponentModel.DataAnnotations;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>Buyer Request For Quote (§4.4). Maps from the old PurchaseRequest with added terms/deadline/status.</summary>
public class Rfq
{
    public int Id { get; set; }

    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;

    [Required]
    public decimal Quantity { get; set; }

    public string? DeliveryTerms { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }

    /// <summary>Bid-collection deadline.</summary>
    public DateTime Deadline { get; set; }

    public RfqStatus Status { get; set; } = RfqStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Competition? Competition { get; set; }
}

/// <summary>The reverse-auction opened for an RFQ. Clock is server-authoritative (BR-8).</summary>
public class Competition
{
    public int Id { get; set; }

    public int RfqId { get; set; }
    public Rfq Rfq { get; set; } = null!;

    public DateTime BidWindowStart { get; set; }
    public DateTime BidWindowEnd { get; set; }

    public CompetitionStatus Status { get; set; } = CompetitionStatus.Open;

    /// <summary>BR-11: default confidential — only "a competition is open" + aggregate stats are public.</summary>
    public CompetitionVisibility Visibility { get; set; } = CompetitionVisibility.Confidential;

    public int? WinningBidId { get; set; }
    public Bid? WinningBid { get; set; }

    /// <summary>Set once when the window is closed — closing is exactly-once (BR-8).</summary>
    public DateTime? ClosedAt { get; set; }
    public DateTime? AwardedAt { get; set; }

    /// <summary>True once seller identities have been revealed to the buyer at shortlist stage (BR-6, audited).</summary>
    public bool IdentitiesRevealed { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Shortlist> Shortlists { get; set; } = new List<Shortlist>();
    public Transaction? Transaction { get; set; }
}

/// <summary>A seller's offer in a competition. Anonymous to the buyer until reveal (BR-3).</summary>
public class Bid
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    [Required]
    public decimal Price { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public int CurrentVersion { get; set; } = 1;
    public bool IsWithdrawn { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BidRevision> Revisions { get; set; } = new List<BidRevision>();
}

/// <summary>Immutable versioned history of a bid (BR-7). Never updated or deleted.</summary>
public class BidRevision
{
    public int Id { get; set; }

    public int BidId { get; set; }
    public Bid Bid { get; set; } = null!;

    public int Version { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Buyer's top-3 shortlist; only then are seller details revealed (BR-6).</summary>
public class Shortlist
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public int BidId { get; set; }
    public Bid Bid { get; set; } = null!;

    public int Rank { get; set; } // 1..3
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
