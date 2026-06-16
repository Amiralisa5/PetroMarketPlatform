using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public record AuthTokens(string AccessToken, string RefreshToken, DateTime AccessExpiresAt,
    int UserId, string[] Roles, string[] Permissions);

/// <summary>Issues signed short-lived JWT access tokens + rotating refresh tokens (§7).</summary>
public interface ITokenService
{
    Task<AuthTokens> IssueAsync(User user, CancellationToken ct = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}

public class TokenService : ITokenService
{
    private readonly PetroContext _db;
    private readonly JwtOptions _jwt;

    public TokenService(PetroContext db, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<AuthTokens> IssueAsync(User user, CancellationToken ct = default)
    {
        // Disabled accounts cannot obtain tokens (covers both login and refresh paths).
        if (!user.IsActive)
            throw AppException.Forbidden("This account is disabled.");

        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id, ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(AppClaims.Mobile, user.Mobile),
            new(AppClaims.KycStatus, user.KycStatus.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(AppClaims.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Rotating refresh token, stored hashed.
        var refreshRaw = Hashing.RandomToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hashing.Sha256(refreshRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });
        await _db.SaveChangesAsync(ct);

        return new AuthTokens(accessToken, refreshRaw, expires, user.Id, roles, permissions);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hashing.Sha256(refreshToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new AppException("Invalid refresh token", 401, "invalid_token");

        if (!existing.IsActive)
            throw new AppException("Refresh token expired or revoked", 401, "invalid_token");

        // Rotate: revoke the old token, issue a fresh pair.
        existing.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await IssueAsync(existing.User, ct);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hashing.Sha256(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is { RevokedAt: null })
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<(string[] roles, string[] permissions)> LoadRolesAndPermissionsAsync(int userId, CancellationToken ct)
    {
        var roleIds = await _db.UserRoles.Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId).ToListAsync(ct);

        var roles = await _db.Roles.Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name).ToArrayAsync(ct);

        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToArrayAsync(ct);

        return (roles, permissions);
    }
}
