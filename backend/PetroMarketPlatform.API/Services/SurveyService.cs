using Microsoft.EntityFrameworkCore;
using PetroMarketPlatform.API.Common;
using PetroMarketPlatform.API.Data;
using PetroMarketPlatform.API.Models;

namespace PetroMarketPlatform.API.Services;

public record SurveyInput(int Score, int OnTimeScore, int InfoAccuracyScore, string? Comments);

public interface ISurveyService
{
    Task<List<Survey>> GetMyPendingAsync(int userId, CancellationToken ct = default);
    Task<Survey> SubmitAsync(int userId, int transactionId, SurveyInput input, CancellationToken ct = default);
    /// <summary>BR-9: an outstanding survey blocks the user's future transactions.</summary>
    Task EnsureNoOutstandingAsync(int userId, CancellationToken ct = default);
}

public class SurveyService : ISurveyService
{
    private readonly PetroContext _db;
    private readonly IRatingService _rating;
    private readonly IAuditService _audit;

    public SurveyService(PetroContext db, IRatingService rating, IAuditService audit)
    {
        _db = db;
        _rating = rating;
        _audit = audit;
    }

    public Task<List<Survey>> GetMyPendingAsync(int userId, CancellationToken ct = default) =>
        _db.Surveys.Include(s => s.Transaction)
            .Where(s => s.RaterId == userId && !s.Completed)
            .ToListAsync(ct);

    public async Task<Survey> SubmitAsync(int userId, int transactionId, SurveyInput input, CancellationToken ct = default)
    {
        foreach (var v in new[] { input.Score, input.OnTimeScore, input.InfoAccuracyScore })
            if (v is < 1 or > 5) throw new AppException("Scores must be between 1 and 5.");

        var survey = await _db.Surveys
            .FirstOrDefaultAsync(s => s.TransactionId == transactionId && s.RaterId == userId, ct)
            ?? throw AppException.NotFound("No survey pending for you on this transaction.");

        if (survey.Completed)
            throw AppException.Conflict("You have already submitted this survey.");

        survey.Score = input.Score;
        survey.OnTimeScore = input.OnTimeScore;
        survey.InfoAccuracyScore = input.InfoAccuracyScore;
        survey.Comments = input.Comments;
        survey.Completed = true;
        survey.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("survey.submit", "Survey", survey.Id, actorId: userId, ct: ct);

        // Recompute the ratee's reputation immediately (BR-10).
        await _rating.RecomputeAsync(survey.RateeId, ct);
        return survey;
    }

    public async Task EnsureNoOutstandingAsync(int userId, CancellationToken ct = default)
    {
        var outstanding = await _db.Surveys.AnyAsync(s => s.RaterId == userId && !s.Completed, ct);
        if (outstanding)
            throw AppException.Forbidden(
                "You have an outstanding post-trade survey. Complete it before starting a new transaction (BR-9).");
    }
}
