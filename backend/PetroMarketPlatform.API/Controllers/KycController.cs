using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetroMarketPlatform.API.Auth;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Dtos;
using PetroMarketPlatform.API.Models;
using PetroMarketPlatform.API.Services;

namespace PetroMarketPlatform.API.Controllers;

/// <summary>KYC submission (§4.1) and the operator review queue (§4.7). Trading is gated by approval (BR-2).</summary>
[Route("api/kyc")]
[Authorize]
public class KycController : ApiControllerBase
{
    private readonly IKycService _kyc;
    private readonly IStorageService _storage;

    public KycController(IKycService kyc, IStorageService storage)
    {
        _kyc = kyc;
        _storage = storage;
    }

    [HttpGet("me")]
    [HasPermission(Permissions.KycComplete)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var c = await _kyc.GetMyCaseAsync(CurrentUserId, ct);
        return c is null ? Ok(null) : Ok(Map(c));
    }

    [HttpPost("company")]
    [HasPermission(Permissions.KycComplete)]
    public async Task<IActionResult> UpsertCompany(CompanyDto dto, CancellationToken ct)
    {
        var company = await _kyc.UpsertCompanyAsync(CurrentUserId,
            new CompanyInput(dto.LegalName, dto.RegistrationNumber, dto.NationalId, dto.Address), ct);
        return Ok(new { company.Id, company.LegalName });
    }

    [HttpPost("documents")]
    [HasPermission(Permissions.KycComplete)]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadDocument([FromForm] string type, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new AppException("A file is required.");
        await using var stream = file.OpenReadStream();
        var key = await _storage.SaveAsync($"user-{CurrentUserId}", file.FileName, stream, ct);
        var doc = await _kyc.AddDocumentAsync(CurrentUserId, type, key, file.FileName, file.ContentType, ct);
        return Ok(new KycDocumentDto(doc.Id, doc.Type, doc.FileName, doc.UploadedAt));
    }

    [HttpPost("submit")]
    [HasPermission(Permissions.KycComplete)]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        await _kyc.SubmitAsync(CurrentUserId, ct);
        return NoContent();
    }

    // ---- Operator queue ----
    [HttpGet("queue")]
    [HasPermission(Permissions.KycReview)]
    public async Task<IActionResult> Queue(CancellationToken ct)
    {
        var cases = await _kyc.GetQueueAsync(ct);
        return Ok(cases.Select(Map));
    }

    [HttpPost("{caseId:int}/review")]
    [HasPermission(Permissions.KycReview)]
    public async Task<IActionResult> Review(int caseId, ReviewDto dto, CancellationToken ct)
    {
        await _kyc.ReviewAsync(CurrentUserId, caseId, dto.Decision, dto.Notes, ct);
        return NoContent();
    }

    private static KycCaseDto Map(KycCase c) => new(
        c.Id, c.UserId, c.User?.FullName, c.Status.ToString(), c.CompanyId, c.Company?.LegalName,
        c.Notes, c.UpdatedAt,
        c.Documents.Select(d => new KycDocumentDto(d.Id, d.Type, d.FileName, d.UploadedAt)).ToList());
}
