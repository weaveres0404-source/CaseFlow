
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Services;
using CaseFlow.Server.Middleware;

namespace CaseFlow.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Npgsql v6+: allow DateTime.UtcNow to write to 'timestamp without time zone' columns
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    // 所有 DateTime 序列化時附加 +08:00 偏移，前端可正確識別為 UTC+8，避免二次時區轉換
                    o.JsonSerializerOptions.Converters.Add(new CaseFlow.Server.Helpers.UtcDateTimeConverter());
                    o.JsonSerializerOptions.Converters.Add(new CaseFlow.Server.Helpers.UtcNullableDateTimeConverter());
                });

            builder.Services.AddSingleton<GcsStorageService>();

            builder.Services.AddOpenApi();

            builder.Logging.ClearProviders();

            // ── Cloud Run: JSON structured logging (single-line per entry → Cloud Logging parses correctly) ──
            // In production every log entry including stack traces is written as ONE JSON line.
            // Cloud Logging then shows it as a structured entry with proper severity.
            if (!builder.Environment.IsDevelopment())
            {
                builder.Logging.AddJsonConsole(opts =>
                {
                    opts.IncludeScopes = false;
                    opts.UseUtcTimestamp = true;
                    opts.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
                });
            }
            else
            {
                builder.Logging.AddConsole();
            }

            // ── Suppress noisy EF command / infrastructure logs in all environments ──
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var conn = connectionString ?? Environment.GetEnvironmentVariable("CASEFLOW_CONNECTION") ?? "Host=localhost;Database=CaseFlowDB;Username=postgres;Password=weaveres0404";
            builder.Services.AddDbContext<CaseFlowDbContext>(options => options.UseNpgsql(conn));

            // ── Data Protection: persist keys to PostgreSQL ──
            // Cloud Run has multiple instances; each would generate its own in-memory key ring
            // without this. When the OAuth challenge is issued on instance A and the Google
            // callback arrives on instance B, StateDataFormat.Unprotect(state) returns null
            // → "The oauth state was missing or invalid".
            // PersistKeysToDbContext writes/reads the key ring from the shared PostgreSQL
            // database so all instances share the same keys.
            builder.Services.AddDataProtection()
                .SetApplicationName("CaseFlow")
                .PersistKeysToDbContext<CaseFlowDbContext>();

            builder.Services
                .AddAuthentication(options =>
                {
                    // API uses Bearer JWT for both authenticate and challenge.
                    // 401 on protected endpoints will return JSON, not redirect.
                    options.DefaultScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    var jwtKey = builder.Configuration["Jwt:Key"]
                        ?? "super_secret_demo_key_2026_long_enough_for_hs256_!!!!!!!!!!!!!!!!";
                    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CaseFlow";
                    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CaseFlowClients";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "caseflow_auth";

                    options.Cookie.HttpOnly = true;

                    options.Cookie.SameSite =
                        SameSiteMode.None;

                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.Always;
                })
                .AddGoogle(options =>
                {
                    options.ClientId =
                        builder.Configuration["Google:ClientId"] ?? "";

                    options.ClientSecret =
                        builder.Configuration["Google:ClientSecret"] ?? "";

                    options.CallbackPath =
                        "/signin-google";

                    options.SaveTokens = true;

                    options.CorrelationCookie.SameSite =
                        SameSiteMode.None;

                    options.CorrelationCookie.SecurePolicy =
                        CookieSecurePolicy.Always;

                    // Redirect gracefully instead of surfacing a 500 when Google
                    // returns an error or the correlation/state validation fails.
                    options.Events.OnRemoteFailure = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();

                        var req = ctx.Request;
                        var hasState = req.Query.ContainsKey("state");
                        var hasCode = req.Query.ContainsKey("code");
                        var googleError = req.Query["error"].ToString();

                        // The correlation cookie name pattern used by OAuthHandler:
                        // .AspNetCore.Correlation.<scheme>.<correlationId>
                        var correlationCookies = req.Cookies.Keys
                            .Where(k => k.StartsWith(".AspNetCore.Correlation"))
                            .ToList();

                        logger.LogError(ctx.Failure,
                            "Google OAuth remote failure. " +
                            "Message={Message}; HasStateQS={HasState}; HasCodeQS={HasCode}; " +
                            "GoogleErrorQS={GoogleError}; CorrelationCookieCount={CCount}; " +
                            "CorrelationCookies=[{CNames}]; ReferrerHost={Referrer}",
                            ctx.Failure?.Message,
                            hasState, hasCode, googleError,
                            correlationCookies.Count,
                            string.Join(",", correlationCookies),
                            req.Headers["Referer"].ToString());

                        ctx.Response.Redirect("/login?error=oauth_failure");
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            app.UseCookiePolicy();

            // ── Global exception handler: returns HTTP 500 JSON body ──
            // Exception is already logged (Console.Error + logger.LogError) by ExceptionLoggingMiddleware
            // which runs inside this handler. This lambda only produces the HTTP response.

            app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
            {
                var exFeature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                var ex = exFeature?.Error;

                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";

                var isDev = app.Environment.IsDevelopment();
                object payload = isDev
                    ? new
                    {
                        success = false,
                        error = new
                        {
                            code = "INTERNAL_ERROR",
                            message = ex?.Message,
                            exception_type = ex?.GetType().FullName,
                            stack_trace = ex?.ToString()
                        }
                    }
                    : (object)new
                    {
                        success = false,
                        error = new
                        {
                            code = "INTERNAL_ERROR",
                            message = "An unexpected error occurred. See Cloud Run logs for details."
                        }
                    };

                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
            }));

            // ── ExceptionLoggingMiddleware: log BEFORE UseExceptionHandler swallows the exception ──
            // Order matters: this runs INSIDE the UseExceptionHandler try-catch.
            // It logs via Console.Error (guaranteed stderr) + ILogger (JSON), then re-throws
            // so UseExceptionHandler above can return the HTTP 500 response.
            app.UseMiddleware<ExceptionLoggingMiddleware>();

            app.UseDefaultFiles();

            app.Use(async (context, next) =>
            {
                var acceptsHtml = context.Request.Headers.Accept
                    .Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);

                if (HttpMethods.IsGet(context.Request.Method) && acceptsHtml)
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                        context.Response.Headers.Pragma = "no-cache";
                        context.Response.Headers.Expires = "0";
                        return Task.CompletedTask;
                    });
                }

                await next();
            });

            app.MapStaticAssets();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Request.Scheme is always "https" in production (forced above).
            // UseHttpsRedirection is dev-only: in production it would be a no-op, but
            // keeping it dev-only avoids any edge-case redirect loops on Cloud Run
            // if ForwardedHeaders/force-HTTPS middleware ever runs out of order.
            if (app.Environment.IsDevelopment())
                app.UseHttpsRedirection();

            // Authentication must come before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapGet("/health", () => Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow
            }));

            app.MapFallbackToFile("/index.html");

            // ── Startup: ensure DataProtectionKeys table exists ──
            // Idempotent — CREATE TABLE IF NOT EXISTS.
            // Kept outside EF migrations to avoid snapshot churn; the table schema
            // is stable and owned by the DataProtection subsystem, not domain migrations.
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CaseFlowDbContext>();
                var dpLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    await db.Database.ExecuteSqlRawAsync("""
                        CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
                            "Id"           integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "FriendlyName" text,
                            "Xml"          text
                        )
                        """);
                    var keyCount = await db.DataProtectionKeys.CountAsync();
                    dpLogger.LogInformation(
                        "DataProtection: table ready, {Count} key(s) in DB", keyCount);
                }
                catch (Exception ex)
                {
                    // CRITICAL: without persisted keys every Cloud Run instance generates
                    // its own ephemeral key ring → "oauth state was missing or invalid"
                    dpLogger.LogCritical(ex,
                        "DataProtectionKeys table setup FAILED – OAuth will break on multi-instance Cloud Run");
                }
            }

            // ── Startup: read-only schema diagnostic (logs issues to stderr, NO DDL) ──
            using (var scope = app.Services.CreateScope())
            {
                var diagLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var db = scope.ServiceProvider.GetRequiredService<CaseFlowDbContext>();
                await SchemaBootstrapper.RunDiagnosticAsync(db, diagLogger);
            }

            app.Run();
        }
    }
}
