using System.Security.Claims;

namespace CaseFlow.Server.Middleware;

public sealed class CustomerReadOnlyMiddleware
{
    private readonly RequestDelegate _next;

    public CustomerReadOnlyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, "Customer", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        var isAllowedRead = HttpMethods.IsGet(context.Request.Method)
            && (path.StartsWithSegments("/api/v1/cases")
                || path.Equals("/api/v1/meta/dropdowns")
                || path.Equals("/api/v1/auth/me")
                || path.StartsWithSegments("/api/v1/attachments"));

        if (isAllowedRead || !path.StartsWithSegments("/api/v1"))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            error = new
            {
                code = "CUSTOMER_READ_ONLY",
                message = "客戶使用者只能查詢所屬專案案件"
            }
        });
    }
}
