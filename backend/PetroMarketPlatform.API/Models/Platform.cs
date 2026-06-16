using System.ComponentModel.DataAnnotations;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>A notification routed through the single notification service (§4.9).</summary>
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationChannel Channel { get; set; }

    /// <summary>Event type, e.g. otp, kyc_decision, rfq_match, bid_status, deadline, winner, survey_reminder.</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? Payload { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>Audit trail of privileged and identity-reveal actions (§7). Append-only.</summary>
public class AuditLog
{
    public int Id { get; set; }

    public int? ActorId { get; set; }
    public User? Actor { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    public string? Ip { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
