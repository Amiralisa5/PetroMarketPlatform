namespace PetroMarketPlatform.API.Common;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "Petki.ir";
    public string Audience { get; set; } = "Petki.ir";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}

public class OtpOptions
{
    public const string Section = "Otp";
    public int Length { get; set; } = 5;
    public int TtlSeconds { get; set; } = 120;
    public int MaxRequestsPerHour { get; set; } = 5;
    public int MaxVerifyAttempts { get; set; } = 5;
    /// <summary>Dev convenience: return the OTP code in the API response. NEVER enable in production.</summary>
    public bool DevExposeCode { get; set; } = false;
}

public class SmsOptions
{
    public const string Section = "Sms";
    /// <summary>Console | Kavenegar | SmsDotIr | IPPanel — only Console is wired in this build.</summary>
    public string Provider { get; set; } = "Console";
    public string SenderId { get; set; } = "Petki";
    public string? ApiKey { get; set; }
}

public class StorageOptions
{
    public const string Section = "Storage";
    /// <summary>Local | S3 — only Local is wired in this build (S3 via the same interface for ArvanCloud/MinIO).</summary>
    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = "kyc-storage";
}

public class RatingWeights
{
    public double Survey { get; set; } = 0.45;
    public double SuccessfulTransactions { get; set; } = 0.20;
    public double OnTime { get; set; } = 0.15;
    public double InfoAccuracy { get; set; } = 0.10;
    public double Complaints { get; set; } = 0.10;
}

public class RatingOptions
{
    public const string Section = "Rating";
    public RatingWeights Weights { get; set; } = new();
}
