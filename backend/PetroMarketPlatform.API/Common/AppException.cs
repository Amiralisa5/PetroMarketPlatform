namespace PetroMarketPlatform.API.Common;

/// <summary>Domain/business-rule violation that maps to a 4xx response.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string message, int statusCode = 400, string code = "bad_request")
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public static AppException NotFound(string what = "Resource not found") => new(what, 404, "not_found");
    public static AppException Forbidden(string what = "Forbidden") => new(what, 403, "forbidden");
    public static AppException Conflict(string what = "Conflict") => new(what, 409, "conflict");
}
