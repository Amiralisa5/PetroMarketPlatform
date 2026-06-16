using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Dtos;
using PetroMarketPlatform.API.Services;

namespace PetroMarketPlatform.API.Controllers;

/// <summary>Mobile + OTP registration and login (BR-1), and JWT refresh/logout (§7).</summary>
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;
    private readonly ITokenService _tokens;
    private readonly PetroContext _db;

    public AuthController(IAuthService auth, ITokenService tokens, PetroContext db)
    {
        _auth = auth;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("register/request-otp")]
    public async Task<IActionResult> RegisterRequestOtp(OtpRequestDto dto, CancellationToken ct)
        => Ok(await _auth.RequestOtpAsync(dto.Mobile, OtpPurpose.Register, ct));

    [HttpPost("register/verify")]
    public async Task<ActionResult<AuthResponse>> RegisterVerify(VerifyDto dto, CancellationToken ct)
    {
        var t = await _auth.VerifyAsync(dto.Mobile, dto.Code, OtpPurpose.Register, dto.FullName, dto.Roles, ct);
        return Ok(ToResponse(t));
    }

    [HttpPost("login/request-otp")]
    public async Task<IActionResult> LoginRequestOtp(OtpRequestDto dto, CancellationToken ct)
        => Ok(await _auth.RequestOtpAsync(dto.Mobile, OtpPurpose.Login, ct));

    [HttpPost("login/verify")]
    public async Task<ActionResult<AuthResponse>> LoginVerify(VerifyDto dto, CancellationToken ct)
    {
        var t = await _auth.VerifyAsync(dto.Mobile, dto.Code, OtpPurpose.Login, null, null, ct);
        return Ok(ToResponse(t));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshDto dto, CancellationToken ct)
        => Ok(ToResponse(await _tokens.RefreshAsync(dto.RefreshToken, ct)));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshDto dto, CancellationToken ct)
    {
        await _tokens.RevokeAsync(dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);
        if (user is null) return NotFound();
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray();
        var perms = User.FindAll(Auth.AppClaims.Permission).Select(c => c.Value).ToArray();
        return Ok(new MeDto(user.Id, user.Mobile, user.FullName, user.KycStatus.ToString(), roles, perms, user.CompanyId));
    }

    private static AuthResponse ToResponse(AuthTokens t) =>
        new(t.AccessToken, t.RefreshToken, t.AccessExpiresAt, t.UserId, t.Roles, t.Permissions);
}
