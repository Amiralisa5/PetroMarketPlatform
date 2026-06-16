using System.Security.Claims;

namespace PetroMarketPlatform.API.Auth;

/// <summary>Scoped accessor for the authenticated principal.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Mobile { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var raw = _principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Mobile => _principal?.FindFirstValue(AppClaims.Mobile);

    public IReadOnlyCollection<string> Roles =>
        _principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        _principal?.FindAll(AppClaims.Permission).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool HasPermission(string permission) =>
        _principal?.Claims.Any(c => c.Type == AppClaims.Permission && c.Value == permission) ?? false;
}
