using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public interface IRatingService
{
    Task<Rating> RecomputeAsync(int userId, CancellationToken ct = default);
}

/// <summary>
/// BR-10 weighted reputation:
/// 0.45·survey + 0.20·successful_txns + 0.15·on_time + 0.10·info_accuracy − 0.10·complaints,
/// each component normalized to 0–1, result scaled to a 1–5 star display. Weights are tunable (config).
/// </summary>
public class RatingService : IRatingService
{
    private readonly PetroContext _db;
    private readonly RatingWeights _w;

    public RatingService(PetroContext db, IOptions<RatingOptions> opts)
    {
        _db = db;
        _w = opts.Value.Weights;
    }

    public async Task<Rating> RecomputeAsync(int userId, CancellationToken ct = default)
    {
        var surveys = await _db.Surveys
            .Where(s => s.RateeId == userId && s.Completed)
            .ToListAsync(ct);

        var completedTxns = await _db.Transactions.CountAsync(
            t => (t.BuyerId == userId || t.SellerId == userId) && t.Status == TransactionStatus.Completed, ct);

        var complaints = await _db.Transactions.CountAsync(
            t => (t.BuyerId == userId || t.SellerId == userId) && t.Status == TransactionStatus.Disputed, ct);

        double surveyComp = surveys.Count > 0 ? surveys.Average(s => s.Score) / 5.0 : 0;
        double onTimeComp = surveys.Count > 0 ? surveys.Average(s => s.OnTimeScore) / 5.0 : 0;
        double infoComp = surveys.Count > 0 ? surveys.Average(s => s.InfoAccuracyScore) / 5.0 : 0;
        double txnComp = Math.Min(completedTxns / 10.0, 1.0);          // saturates at 10 deals
        double complaintComp = Math.Min(complaints / 5.0, 1.0);        // saturates at 5 complaints

        double weighted =
            _w.Survey * surveyComp +
            _w.SuccessfulTransactions * txnComp +
            _w.OnTime * onTimeComp +
            _w.InfoAccuracy * infoComp -
            _w.Complaints * complaintComp;
        weighted = Math.Clamp(weighted, 0, 1);

        bool hasData = surveys.Count > 0 || completedTxns > 0;
        double star = hasData ? Math.Round(weighted * 5.0, 2) : 0;

        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.SubjectUserId == userId, ct);
        if (rating is null)
        {
            rating = new Rating { SubjectUserId = userId };
            _db.Ratings.Add(rating);
        }
        rating.StarScore = star;
        rating.WeightedScore = Math.Round(weighted, 4);
        rating.UpdatedAt = DateTime.UtcNow;
        rating.MetricsJson = JsonSerializer.Serialize(new
        {
            surveyCount = surveys.Count,
            surveyAvg = Math.Round(surveyComp * 5, 2),
            completedTransactions = completedTxns,
            onTimeAvg = Math.Round(onTimeComp * 5, 2),
            infoAccuracyAvg = Math.Round(infoComp * 5, 2),
            complaints
        });
        await _db.SaveChangesAsync(ct);
        return rating;
    }
}
