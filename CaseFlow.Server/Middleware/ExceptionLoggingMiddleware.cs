using System.Text.Json;

namespace CaseFlow.Server.Middleware;

/// <summary>
/// Catches ALL unhandled exceptions from the controller pipeline and ensures
/// they are visible in Cloud Run Logs Explorer, using two complementary methods:
///
///   1. Console.Error.WriteLine — raw single-line JSON to stderr.
///      Cloud Run ingests every line written to stderr as a separate log entry.
///      This is the *guaranteed* path: it works even if the ASP.NET Core
///      logger itself is misconfigured.
///
///   2. logger.LogError(ex, ...) — structured log through the normal
///      ILogger pipeline (picked up by AddJsonConsole / Cloud Logging).
///
/// After logging the exception is re-thrown so that UseExceptionHandler
/// can return the HTTP 500 response to the client.
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next,
                                      ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "/";
            var traceId = context.TraceIdentifier;
            var exType = ex.GetType().FullName ?? "Exception";

            // ── PATH 1: Direct stderr write (Cloud Run always captures this) ──
            // Write a single-line JSON that Cloud Logging parses as severity=ERROR.
            // Cloud Logging structured-logging special fields:
            //   "severity"   → maps to log entry severity
            //   "message"    → shown as the primary log message
            // https://cloud.google.com/logging/docs/structured-logging
            var stderrEntry = JsonSerializer.Serialize(new
            {
                severity = "ERROR",
                message = $"[UNHANDLED] {method} {path} | {exType}: {ex.Message}",
                exception = ex.ToString(),          // full stack trace in one field
                http_request = new
                {
                    request_method = method,
                    request_url = path
                },
                trace_id = traceId,
                timestamp = DateTime.UtcNow.ToString("O")
            });
            Console.Error.WriteLine(stderrEntry);   // single line → single Cloud Logging entry

            // ── PATH 2: ASP.NET Core ILogger (AddJsonConsole picks this up) ──
            _logger.LogError(ex,
                "[UNHANDLED] {Method} {Path} | {ExType}: {ExMessage}",
                method, path, exType, ex.Message);

            // Re-throw: let UseExceptionHandler return the HTTP 500 body.
            throw;
        }
    }
}
