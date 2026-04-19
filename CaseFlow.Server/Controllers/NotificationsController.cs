using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public NotificationsController(CaseFlowDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/notifications?is_read=false&page=1&page_size=20
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int page_size = 20,
            [FromQuery] bool? is_read = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var userId = User.GetUserId();

            var query = _db.Notifications.AsNoTracking()
                .Where(n => n.RecipientUserId == userId)
                .AsQueryable();

            if (is_read.HasValue)
                query = query.Where(n => n.IsRead == is_read.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(n => new
                {
                    id = n.NotificationId,
                    notification_type = n.NotificationType,
                    title = n.Title,
                    message = n.Message,
                    case_id = n.CaseId,
                    is_read = n.IsRead,
                    created_at = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        // PATCH /api/v1/notifications/read — 批次已讀
        [HttpPatch("read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkReadDto dto)
        {
            var userId = User.GetUserId();

            if (dto.NotificationIds != null && dto.NotificationIds.Count > 0)
            {
                var notifications = await _db.Notifications
                    .Where(n => dto.NotificationIds.Contains(n.NotificationId) && n.RecipientUserId == userId && !n.IsRead)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                foreach (var n in notifications)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
            }
            else if (dto.All == true)
            {
                var notifications = await _db.Notifications
                    .Where(n => n.RecipientUserId == userId && !n.IsRead)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                foreach (var n in notifications)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
            }
            else
            {
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Provide notification_ids or all:true" } });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class MarkReadDto
    {
        public List<int>? NotificationIds { get; set; }
        public bool? All { get; set; }
    }
}
