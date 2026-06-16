using System.Text.Json;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

/// <summary>Writes the append-only audit trail (§7). Privileged + identity-reveal actions must be logged.</summary>
public interface IAuditService
{
    Task LogAsync(string action, string? entity = null, object? entityId = null,
        object? before = null, object? after = null, int? actorId = null, CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly PetroContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(PetroContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? entity = null, object? entityId = null,
        object? before = null, object? after = null, int? actorId = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            ActorId = actorId,
            Action = action,
            Entity = entity,
            EntityId = entityId?.ToString(),
            Before = before is null ? null : JsonSerializer.Serialize(before),
            After = after is null ? null : JsonSerializer.Serialize(after),
            Ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
