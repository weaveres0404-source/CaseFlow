using System.Security.Claims;

namespace CaseFlow.Server.Helpers
{
    public class CurrentUser
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "SE";
        public string Email { get; set; } = "";
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("user_id");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        public static string GetRole(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Role);
            return claim?.Value ?? "SE";
        }

        /// <summary>是否為管理員（SysAdmin 或 Admin 皆視為系統管理員）</summary>
        public static bool IsAdminRole(this ClaimsPrincipal user)
        {
            var role = user.GetRole();
            return role == "SysAdmin" || role == "Admin";
        }
    }
}
