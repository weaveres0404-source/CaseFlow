using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;
using Microsoft.AspNetCore.Identity;
using Google.Apis.Auth;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly CaseFlowDbContext _db;

        public AuthController(IConfiguration config, CaseFlowDbContext db)
        {
            _config = config;
            _db = db;
        }

        // GET /api/v1/auth/config  —  前端用來判斷顯示哪種登入方式
        [AllowAnonymous]
        [HttpGet("config")]
        public IActionResult GetAuthConfig()
        {
            var mode = (_config["Auth:Mode"] ?? "both").Trim().ToLowerInvariant();
            if (mode != "password" && mode != "google" && mode != "both")
                mode = "both";

            var googleClientId = _config["Google:ClientId"] ?? "";

            return Ok(new
            {
                success = true,
                data = new
                {
                    mode,
                    google_client_id = googleClientId
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var mode = (_config["Auth:Mode"] ?? "both").Trim().ToLowerInvariant();
            if (mode == "google")
                return StatusCode(403, new { success = false, error = new { code = "AUTH_MODE_DISABLED", message = "Password login is disabled on this environment" } });

            var username = req?.Username?.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(req?.Password))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Username and password required" } });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.IsActive && u.Username == username);
            if (user == null)
                return Ok(new { success = false, error = new { code = "UNAUTHORIZED", message = "Invalid credentials" } });

            // 驗證密碼（支援舊 plaintext 自動升級為 hash）
            var hasher = new PasswordHasher<User>();
            PasswordVerificationResult verifyResult;
            bool wasPlaintext = false;

            try
            {
                verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password!);
            }
            catch
            {
                verifyResult = PasswordVerificationResult.Failed;
            }

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                // Legacy plaintext fallback
                if (user.PasswordHash == req.Password!)
                {
                    wasPlaintext = true;
                }
                else
                {
                    return Ok(new { success = false, error = new { code = "UNAUTHORIZED", message = "Invalid credentials" } });
                }
            }

            var now = TimeHelper.Now;

            // 自動升級 plaintext 密碼或 rehash（在 SaveChanges 前統一處理）
            if (wasPlaintext || verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = hasher.HashPassword(user, req.Password!);

            user.LastLoginAt = now;
            await _db.SaveChangesAsync();

            // 首次登入強制改密碼：回 setup_token（scope=setup, 10 分鐘有效）
            if (user.MustChangePassword)
            {
                var setupToken = GenerateSetupToken(user);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        must_change_password = true,
                        setup_token = setupToken,
                        expires_in = 600
                    }
                });
            }

            var tokenString = GenerateJwt(user);

            return Ok(new
            {
                success = true,
                data = new
                {
                    must_change_password = false,
                    access_token = tokenString,
                    token_type = "Bearer",
                    expires_in = 60 * 60 * 8,
                    user = new { user_id = user.UserId, username = user.Username, full_name = user.FullName, role = user.Role }
                }
            });
        }

        // POST /api/v1/auth/google-login
        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
        {
            var mode = (_config["Auth:Mode"] ?? "both").Trim().ToLowerInvariant();
            if (mode == "password")
                return StatusCode(403, new { success = false, error = new { code = "AUTH_MODE_DISABLED", message = "Google login is disabled on this environment" } });

            if (string.IsNullOrWhiteSpace(req?.IdToken))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "id_token is required" } });

            var clientId = _config["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                return StatusCode(503, new { success = false, error = new { code = "CONFIG_ERROR", message = "Google login is not configured on this server" } });

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { success = false, error = new { code = "INVALID_TOKEN", message = "Invalid or expired Google token" } });
            }

            if (!payload.EmailVerified)
                return Unauthorized(new { success = false, error = new { code = "EMAIL_NOT_VERIFIED", message = "Google account email is not verified" } });

            var googleSub = payload.Subject;
            var googleEmail = payload.Email;
            var googleName = payload.Name;

            // 依序找：google_sub → google_email → email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub)
                       ?? await _db.Users.FirstOrDefaultAsync(u => u.IsActive && u.GoogleEmail == googleEmail)
                       ?? await _db.Users.FirstOrDefaultAsync(u => u.IsActive && u.Email == googleEmail);

            var now = TimeHelper.Now;

            if (user == null)
            {
                return Unauthorized(new { success = false, error = new { code = "ACCOUNT_NOT_REGISTERED", message = "This Google account is not registered in CaseFlow" } });
            }

            if (!user.IsActive)
                return Unauthorized(new { success = false, error = new { code = "ACCOUNT_INACTIVE", message = "Account is inactive" } });

            await GoogleUserProfileSync.SyncAsync(_db, user, googleSub, googleEmail, googleName, now);

            var tokenString = GenerateJwt(user);
            return Ok(new
            {
                success = true,
                data = new
                {
                    access_token = tokenString,
                    token_type = "Bearer",
                    expires_in = 60 * 60 * 8,
                    user = new { user_id = user.UserId, username = user.Username, full_name = user.FullName, role = user.Role, auth_provider = user.AuthProvider }
                }
            });
        }

        // POST /api/v1/auth/setup-password  —  首次登入設定新密碼
        [AllowAnonymous]
        [HttpPost("setup-password")]
        public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.SetupToken) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "setup_token and new_password are required" } });

            if (req.NewPassword.Length < 8)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "new_password must be at least 8 characters" } });

            // 驗證 setup token 並確認 scope=setup
            int userId;
            try
            {
                var jwtKey = _config["Jwt:Key"] ?? "super_secret_demo_key_2026_long_enough_for_hs256_!!!!!!!!!!!!!!!!";
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(req.SetupToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"] ?? "CaseFlow",
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"] ?? "CaseFlowClients",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                }, out _);

                var scopeClaim = principal.FindFirst("scope")?.Value;
                if (scopeClaim != "setup")
                    return Unauthorized(new { success = false, error = new { code = "PERMISSION_DENIED", message = "Invalid setup token scope" } });

                userId = int.Parse(principal.FindFirst("user_id")!.Value);
            }
            catch
            {
                return Unauthorized(new { success = false, error = new { code = "UNAUTHORIZED", message = "Invalid or expired setup token" } });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
            if (user == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "User not found" } });

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, req.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = TimeHelper.Now;
            await _db.SaveChangesAsync();

            var accessToken = GenerateJwt(user);
            return Ok(new
            {
                success = true,
                data = new
                {
                    must_change_password = false,
                    access_token = accessToken,
                    token_type = "Bearer",
                    expires_in = 60 * 60 * 8,
                    user = new { user_id = user.UserId, username = user.Username, full_name = user.FullName, role = user.Role }
                }
            });
        }

        // GET /api/v1/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.GetUserId();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "User not found" } });

            return Ok(new
            {
                success = true,
                data = new
                {
                    user_id = user.UserId,
                    username = user.Username,
                    full_name = user.FullName,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role,
                    is_active = user.IsActive,
                    last_login_at = user.LastLoginAt
                }
            });
        }

        private string GenerateJwt(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "super_secret_demo_key_2026_long_enough_for_hs256_!!!!!!!!!!!!!!!!";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "CaseFlow";
            var jwtAudience = _config["Jwt:Audience"] ?? "CaseFlowClients";

            var claims = new List<Claim>
            {
                new Claim("user_id", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "SE"),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateSetupToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "super_secret_demo_key_2026_long_enough_for_hs256_!!!!!!!!!!!!!!!!";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "CaseFlow";
            var jwtAudience = _config["Jwt:Audience"] ?? "CaseFlowClients";

            var claims = new List<Claim>
            {
                new Claim("user_id", user.UserId.ToString()),
                new Claim("scope", "setup")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = "";
    }

    public class SetupPasswordRequest
    {
        public string SetupToken { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
