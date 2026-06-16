using Microsoft.AspNetCore.Mvc;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Authenticated user id, or throws 401.</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : throw new AppException("Not authenticated.", 401, "unauthorized");

    protected int? CurrentUserIdOrNull =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    protected bool HasPerm(string permission) =>
        User.Claims.Any(c => c.Type == Auth.AppClaims.Permission && c.Value == permission);
}
