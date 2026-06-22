using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CaseFlow.Server.Helpers;
using CaseFlow.Server.Models;

namespace CaseFlow.Server.Controllers;

/// <summary>
/// Server-side Google OAuth 2.0 redirect flow.
///
/// Flow:
///   1. Browser ??GET /auth/google/login
///   2. Server   ??Challenge("Google") ??302 to Google consent
///   3. Google   ??GET /signin-google  (ASP.NET Core OAuth middleware callback)
///   4. Middleware signs user into "GoogleOAuthTemp" cookie ??302 to /auth/google/callback
///   5. Server   ??find/create DB user ??generate JWT ??302 to SPA /?access_token=TOKEN
/// </summary>
[ApiController]
[Route("auth/google")]
[AllowAnonymous]
public class GoogleOAuthController : ControllerBase
{
    private readonly CaseFlowDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleOAuthController> _logger;

    public GoogleOAuthController(
        CaseFlowDbContext db,
        IConfiguration config,
        ILogger<GoogleOAuthController> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    // ??? Step 1: Trigger Google OAuth challenge ???????????????????????????????
    // GET /auth/google/login
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        // Validate returnUrl to only allow relative paths (open-redirect prevention)
        var safeReturn = (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") &&
                          !returnUrl.StartsWith("//")) ? returnUrl : "/";

        // Generate an absolute callback URL.
        // Request.Scheme is "https" thanks to ForwardedHeaders middleware processing
        // X-Forwarded-Proto before this action runs.
        var callbackUrl = Url.Action(nameof(Callback), "GoogleOAuth",
            new { returnUrl = safeReturn }, Request.Scheme)!;

        _logger.LogInformation(
            "Google OAuth login initiated. Scheme={Scheme} CallbackUrl={CallbackUrl}",
            Request.Scheme, callbackUrl);

        return Challenge(
            new AuthenticationProperties { RedirectUri = callbackUrl },
            "Google");
    }

    // ??? Step 2: Process Google callback, issue JWT ???????????????????????????
    // GET /auth/google/callback
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl = "/")
    {
        // ?? 1. Validate the OAuth state / temp cookie ????????????????????????
        AuthenticateResult result;
        try
        {
            result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleOAuthTemp AuthenticateAsync threw an exception");
            return Redirect("/login?error=google_auth_failed");
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Google OAuth authentication failed: {Failure}",
                result.Failure?.Message ?? "unknown");
            return Redirect("/login?error=google_auth_failed");
        }

        // ?? 2. Extract claims ????????????????????????????????????????????????
        var principal   = result.Principal!;
        var googleSub   = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var googleEmail = principal.FindFirstValue(ClaimTypes.Email);
        var googleName  = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");

        if (string.IsNullOrEmpty(googleSub) || string.IsNullOrEmpty(googleEmail))
        {
            _logger.LogWarning(
                "Google OAuth: missing claims. Sub={Sub} Email={Email}", googleSub, googleEmail);
            return Redirect("/login?error=google_missing_claims");
        }

        // ?? 3. Find or create user in database ???????????????????????????????
        User? user;
        try
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub)
                   ?? await _db.Users.FirstOrDefaultAsync(u => u.IsActive && u.GoogleEmail == googleEmail)
                   ?? await _db.Users.FirstOrDefaultAsync(u => u.IsActive && u.Email == googleEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Database error looking up user during Google OAuth callback. Email={Email}", googleEmail);
            return Redirect("/login?error=db_error");
        }

        var now = TimeHelper.Now;

        if (user == null)
        {
            _logger.LogWarning("Unregistered Google account attempted login. Email={Email}", googleEmail);
            return Redirect("/login?error=account_not_registered");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user attempted Google login. Email={Email}", googleEmail);
            return Redirect("/login?error=account_inactive");
        }

        try
        {
            await GoogleUserProfileSync.SyncAsync(_db, user, googleSub, googleEmail, googleName, now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error saving user during Google OAuth. Email={Email}", googleEmail);
            return Redirect("/login?error=db_error");
        }

        // ?? 4. Clean up temp cookie, issue JWT, redirect to SPA ??????????????
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var jwt = JwtHelper.GenerateJwt(user, _config);
        var safeReturn = (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") &&
                          !returnUrl.StartsWith("//")) ? returnUrl : "/";

        _logger.LogInformation(
            "Google OAuth login succeeded. UserId={UserId} Email={Email}", user.UserId, user.Email);

        return Redirect($"{safeReturn}?access_token={Uri.EscapeDataString(jwt)}");
    }
}

