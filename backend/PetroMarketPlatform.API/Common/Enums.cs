namespace PetroMarketPlatform.API.Common;

/// <summary>Aggregate KYC state surfaced on the user (mirrors the latest KycCase).</summary>
public enum KycStatus
{
    None,
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    MoreInfo
}

public enum RfqStatus
{
    Open,
    Closed,
    Awarded,
    Cancelled
}

public enum CompetitionStatus
{
    Open,
    Closed,    // bidding window elapsed, awaiting shortlist/winner
    Awarded,
    Cancelled
}

/// <summary>BR-11: per-competition price visibility (default Confidential).</summary>
public enum CompetitionVisibility
{
    Confidential,   // only "a competition is open" is public
    AggregateOnly,  // anonymized aggregate stats public
    Public          // full prices public (opt-in)
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Disputed,
    Cancelled
}

public enum ArticleType
{
    News,
    Analysis
}

public enum NotificationChannel
{
    Sms,
    Push,
    InApp
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Read
}

public enum OtpPurpose
{
    Register,
    Login
}
