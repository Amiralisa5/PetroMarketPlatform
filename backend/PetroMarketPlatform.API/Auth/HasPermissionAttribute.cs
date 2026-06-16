using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PetroMarketPlatform.API.Auth;

/// <summary>
/// Authorizes an action against a named permission carried in the JWT ("perm" claims).
/// Requires an authenticated user; returns 401 if anonymous, 403 if the permission is missing.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission) => _permission = permission;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasPerm = user.Claims.Any(c => c.Type == AppClaims.Permission && c.Value == _permission);
        if (!hasPerm)
        {
            context.Result = new ObjectResult(new { error = "forbidden", required = _permission })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
