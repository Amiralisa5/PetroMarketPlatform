using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Dtos;
using PetroMarketPlatform.API.Models;
using PetroMarketPlatform.API.Services;

namespace PetroMarketPlatform.API.Controllers;

// ============ Admin: users, roles, permissions, KPIs (§4.8) ============
[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    private readonly PetroContext _db;
    private readonly IAuditService _audit;

    public AdminController(PetroContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("users")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> Users([FromQuery] string? q, CancellationToken ct = default)
    {
        var query = _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).Include(u => u.Company).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Mobile.Contains(q) || (u.FullName != null && u.FullName.Contains(q)));
        var rows = await query.OrderBy(u => u.Id).Select(u => new
        {
            u.Id, u.Mobile, u.FullName, KycStatus = u.KycStatus.ToString(), u.IsActive,
            Company = u.Company != null ? u.Company.LegalName : null,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPut("users/{id:int}/roles")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> SetRoles(int id, AssignRolesDto dto, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("User not found.");
        var roles = await _db.Roles.Where(r => dto.Roles.Contains(r.Name)).ToListAsync(ct);

        _db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var r in roles) _db.UserRoles.Add(new UserRole { UserId = id, RoleId = r.Id });
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("admin.set_roles", "User", id, after: new { dto.Roles }, actorId: CurrentUserId, ct: ct);
        return NoContent();
    }

    [HttpPut("users/{id:int}/active")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool active, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw AppException.NotFound("User not found.");
        user.IsActive = active;
        if (!active)
        {
            // Revoke all active refresh tokens so a banned user cannot stay logged in via refresh.
            var now = DateTime.UtcNow;
            await _db.RefreshTokens
                .Where(t => t.UserId == id && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("admin.set_active", "User", id, after: new { active }, actorId: CurrentUserId, ct: ct);
        return NoContent();
    }

    [HttpGet("roles")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> RolesList(CancellationToken ct)
    {
        var rows = await _db.Roles.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Select(r => new { r.Id, r.Name, r.Description, Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList() })
            .ToListAsync(ct);
        return Ok(new { roles = rows, allPermissions = Permissions.All });
    }

    /// <summary>RBAC is editable at runtime — Admin can re-scope a role's permissions without code (§3).</summary>
    [HttpPut("roles/{roleName}/permissions")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> SetRolePermissions(string roleName, SetRolePermissionsDto dto, CancellationToken ct)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw AppException.NotFound("Role not found.");
        var perms = await _db.Permissions.Where(p => dto.Permissions.Contains(p.Name)).ToListAsync(ct);

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var p in perms) _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("admin.set_role_perms", "Role", role.Id, after: new { dto.Permissions }, actorId: CurrentUserId, ct: ct);
        return NoContent();
    }

    /// <summary>Reports & KPIs (§4.8).</summary>
    [HttpGet("kpis")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> Kpis(CancellationToken ct)
    {
        var totalUsers = await _db.Users.CountAsync(ct);
        var approved = await _db.Users.CountAsync(u => u.KycStatus == KycStatus.Approved, ct);
        var pendingKyc = await _db.KycCases.CountAsync(k => k.Status == KycStatus.Submitted || k.Status == KycStatus.UnderReview, ct);
        var totalCompetitions = await _db.Competitions.CountAsync(ct);
        var awarded = await _db.Competitions.CountAsync(c => c.Status == CompetitionStatus.Awarded, ct);
        var openComps = await _db.Competitions.CountAsync(c => c.Status == CompetitionStatus.Open, ct);
        var txns = await _db.Transactions.CountAsync(ct);
        // Compute GMV client-side: SQLite can't SUM decimal server-side.
        var txnRows = await _db.Transactions.Select(t => new { t.Price, t.Quantity }).ToListAsync(ct);
        var gmv = txnRows.Sum(t => t.Price * t.Quantity);

        return Ok(new
        {
            totalUsers, kycApproved = approved, kycPending = pendingKyc,
            totalCompetitions, awardedCompetitions = awarded, openCompetitions = openComps,
            competitionSuccessRate = totalCompetitions == 0 ? 0 : Math.Round((double)awarded / totalCompetitions, 3),
            transactions = txns, grossMerchandiseValue = gmv
        });
    }
}

// ============ Audit trail (§7) ============
[Route("api/audit")]
public class AuditController : ApiControllerBase
{
    private readonly PetroContext _db;
    public AuditController(PetroContext db) => _db = db;

    [HttpGet]
    [HasPermission(Permissions.AuditView)]
    public async Task<IActionResult> List([FromQuery] string? entity, [FromQuery] string? action, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entity)) q = q.Where(a => a.Entity == entity);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);
        var rows = await q.OrderByDescending(a => a.Id).Take(200)
            .Select(a => new { a.Id, a.ActorId, a.Action, a.Entity, a.EntityId, a.Before, a.After, a.Ip, a.CreatedAt })
            .ToListAsync(ct);
        return Ok(rows);
    }
}
