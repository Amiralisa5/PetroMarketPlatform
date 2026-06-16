using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;
using Microsoft.EntityFrameworkCore;

namespace PetroMarketPlatform.API.Services;

/// <summary>
/// Single notification service (§4.9) routing all transactional events through SMS / push / in-app
/// according to per-user preferences. Push is represented as an in-app record in this build.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(int userId, string type, string title, string message,
        bool sms = false, CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly PetroContext _db;
    private readonly ISmsSender _sms;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(PetroContext db, ISmsSender sms, ILogger<NotificationService> log)
    {
        _db = db;
        _sms = sms;
        _log = log;
    }

    public async Task NotifyAsync(int userId, string type, string title, string message,
        bool sms = false, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        // Always create an in-app record (cheap, durable, user-visible).
        if (user.NotifyInApp)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Channel = NotificationChannel.InApp,
                Type = type,
                Title = title,
                Payload = message,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow
            });
        }

        if (sms && user.NotifySms)
        {
            var record = new Notification
            {
                UserId = userId,
                Channel = NotificationChannel.Sms,
                Type = type,
                Title = title,
                Payload = message,
                Status = NotificationStatus.Pending
            };
            _db.Notifications.Add(record);
            try
            {
                var ok = await _sms.SendAsync(user.Mobile, message, ct);
                record.Status = ok ? NotificationStatus.Sent : NotificationStatus.Failed;
                record.SentAt = ok ? DateTime.UtcNow : null;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SMS send failed for user {UserId}", userId);
                record.Status = NotificationStatus.Failed;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
