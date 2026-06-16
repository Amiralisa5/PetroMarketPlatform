using System.ComponentModel.DataAnnotations;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>
/// KYC submission with a status machine: Draft → Submitted → UnderReview → Approved/Rejected/MoreInfo (§4.1).
/// </summary>
public class KycCase
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public KycStatus Status { get; set; } = KycStatus.Draft;

    public int? ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public DateTime? DecidedAt { get; set; }

    /// <summary>Operator reason / compliance notes (required on reject / more-info).</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KycDocument> Documents { get; set; } = new List<KycDocument>();
}

/// <summary>A KYC supporting document. The file itself lives in object storage (object_key).</summary>
public class KycDocument
{
    public int Id { get; set; }

    public int KycCaseId { get; set; }
    public KycCase KycCase { get; set; } = null!;

    [Required]
    public string Type { get; set; } = string.Empty; // national_id | company_registration | other

    [Required]
    public string ObjectKey { get; set; } = string.Empty;

    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
