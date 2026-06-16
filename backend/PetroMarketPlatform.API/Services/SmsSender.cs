using Microsoft.Extensions.Options;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Services;

/// <summary>
/// SMS gateway abstraction (§8 Integrations). Iranian gateways (Kavenegar/SMS.ir/IPPanel) plug in
/// behind this interface; the rest of the app never knows which provider is configured.
/// </summary>
public interface ISmsSender
{
    Task<bool> SendAsync(string mobile, string message, CancellationToken ct = default);
}

/// <summary>Dev provider: logs the SMS instead of sending. Swap for a real gateway in production.</summary>
public class ConsoleSmsSender : ISmsSender
{
    private readonly ILogger<ConsoleSmsSender> _log;
    private readonly SmsOptions _opts;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> log, IOptions<SmsOptions> opts)
    {
        _log = log;
        _opts = opts.Value;
    }

    public Task<bool> SendAsync(string mobile, string message, CancellationToken ct = default)
    {
        _log.LogInformation("[SMS:{Sender} -> {Mobile}] {Message}", _opts.SenderId, mobile, message);
        return Task.FromResult(true);
    }
}
