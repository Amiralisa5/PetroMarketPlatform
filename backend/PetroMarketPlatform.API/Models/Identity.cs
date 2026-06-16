using System.ComponentModel.DataAnnotations;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Models;

/// <summary>A platform account. Registration is mobile + OTP (BR-1); trading is gated by KYC (BR-2).</summary>
public class User
{
    public int Id { get; set; }

    [Required]
    public string Mobile { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public KycStatus KycStatus { get; set; } = KycStatus.None;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsActive { get; set; } = true;

    // Per-user notification preferences (§4.9)
    public bool NotifySms { get; set; } = true;
    public bool NotifyPush { get; set; } = true;
    public bool NotifyInApp { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public Rating? Rating { get; set; }
}

/// <summary>Legal trading entity captured during KYC.</summary>
public class Company
{
    public int Id { get; set; }

    [Required]
    public string LegalName { get; set; } = string.Empty;

    public string? RegistrationNumber { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>RBAC role. Permissions are assignable (not hard-coded per role) — §3.</summary>
public class Role
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>A named, fine-grained permission (e.g. "rfq.create").</summary>
public class Permission
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

/// <summary>One-time password challenge. Rate-limited & expiring (BR-1, §7).</summary>
public class OtpRequest
{
    public int Id { get; set; }

    [Required]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    public string CodeHash { get; set; } = string.Empty;

    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public bool Consumed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Hashed refresh token for rotating, short-lived access tokens (§7).</summary>
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
