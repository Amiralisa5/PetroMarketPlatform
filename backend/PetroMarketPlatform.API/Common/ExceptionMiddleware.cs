using System.Text.Json;

namespace PetroMarketPlatform.API.Common;

/// <summary>Maps AppException to clean JSON 4xx responses; everything else to 500.</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (AppException ex)
        {
            ctx.Response.StatusCode = ex.StatusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Code, message = ex.Message }));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "server_error", message = "An unexpected error occurred." }));
        }
    }
}
