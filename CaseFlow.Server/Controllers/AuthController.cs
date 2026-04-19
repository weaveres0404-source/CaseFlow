using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Email and password required" } });

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == req.Email && u.IsActive);
            if (user == null)
                return Ok(new { success = false, error = new { code = "UNAUTHORIZED", message = "Invalid credentials" } });

            if (user.PasswordHash != req.Password)
                return Ok(new { success = false, error = new { code = "UNAUTHORIZED", message = "Invalid credentials" } });

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

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
    }
}