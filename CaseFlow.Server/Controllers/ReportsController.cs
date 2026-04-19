using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public ReportsController(CaseFlowDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/reports/hours?group_by=se&metric=hours|count&date_from=&date_to=&project_id=...
        [HttpGet("hours")]
        public async Task<IActionResult> GetHours(
            [FromQuery] string group_by = "se",
            [FromQuery] string metric = "hours",
            [FromQuery] int? project_id = null,
            [FromQuery] int? customer_id = null,
            [FromQuery] int? category_id = null,
            [FromQuery] short? status = null,
            [FromQuery] string? case_type = null,
            [FromQuery] int? created_by = null,
            [FromQuery] int? assigned_pm_id = null,
            [FromQuery] int? handler_user_id = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null)
        {
            var userId = User.GetUserId();
            var role = User.GetRole();

            var logsQuery = _db.CaseLogs.AsNoTracking()
                .Include(l => l.Case).ThenInclude(c => c.Project)
                .Include(l => l.Case).ThenInclude(c => c.Customer)
                .Include(l => l.Case).ThenInclude(c => c.Category)
                .Include(l => l.Case).ThenInclude(c => c.CreatedByNavigation)
                .Include(l => l.Case).ThenInclude(c => c.AssignedPm)
                .Include(l => l.HandlerUser)
                .AsQueryable();

            // 權限過濾
            if (role == "PM")
            {
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId).ToListAsync();
                logsQuery = logsQuery.Where(l => myProjectIds.Contains(l.Case.ProjectId));
            }
            else if (role == "SE")
            {
                logsQuery = logsQuery.Where(l => l.HandlerUserId == userId);
            }

            // 篩選
            if (project_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.ProjectId == project_id.Value);
            if (customer_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.CustomerId == customer_id.Value);
            if (category_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.CategoryId == category_id.Value);
            if (status.HasValue) logsQuery = logsQuery.Where(l => l.Case.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(case_type)) logsQuery = logsQuery.Where(l => l.Case.CaseType == case_type);
            if (created_by.HasValue) logsQuery = logsQuery.Where(l => l.Case.CreatedBy == created_by.Value);
            if (assigned_pm_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.AssignedPmId == assigned_pm_id.Value);
            if (handler_user_id.HasValue) logsQuery = logsQuery.Where(l => l.HandlerUserId == handler_user_id.Value);
            if (date_from.HasValue) logsQuery = logsQuery.Where(l => l.Case.CreatedAt >= date_from.Value);
            if (date_to.HasValue) logsQuery = logsQuery.Where(l => l.Case.CreatedAt <= date_to.Value);

            var logs = await logsQuery.ToListAsync();

            object result;

            if (metric == "count")
            {
                // 案件數量統計
                var cases = logs.Select(l => l.Case).DistinctBy(c => c.CaseId).ToList();

                result = group_by switch
                {
                    "status" => cases.GroupBy(c => c.Status).Select(g => new { dimension = g.Key.ToString(), count = g.Count() }).OrderBy(x => x.dimension),
                    "project" => cases.GroupBy(c => c.Project.ProjectName).Select(g => new { dimension = g.Key, count = g.Count() }).OrderBy(x => x.dimension),
                    "customer" => cases.GroupBy(c => c.Customer.CustomerName).Select(g => new { dimension = g.Key, count = g.Count() }).OrderBy(x => x.dimension),
                    "category" => cases.GroupBy(c => c.Category.CategoryName).Select(g => new { dimension = g.Key, count = g.Count() }).OrderBy(x => x.dimension),
                    "created_by" => cases.GroupBy(c => c.CreatedByNavigation.FullName).Select(g => new { dimension = g.Key, count = g.Count() }).OrderBy(x => x.dimension),
                    "assigned_pm" => cases.Where(c => c.AssignedPm != null).GroupBy(c => c.AssignedPm!.FullName).Select(g => new { dimension = g.Key, count = g.Count() }).OrderBy(x => x.dimension),
                    _ => (object)cases.GroupBy(c => "total").Select(g => new { dimension = "total", count = g.Count() })
                };
            }
            else
            {
                // 工時統計
                result = group_by switch
                {
                    "se" => logs.GroupBy(l => l.HandlerUser.FullName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "project" => logs.GroupBy(l => l.Case.Project.ProjectName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "customer" => logs.GroupBy(l => l.Case.Customer.CustomerName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "category" => logs.GroupBy(l => l.Case.Category.CategoryName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "created_by" => logs.GroupBy(l => l.Case.CreatedByNavigation.FullName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "assigned_pm" => logs.Where(l => l.Case.AssignedPm != null).GroupBy(l => l.Case.AssignedPm!.FullName).Select(g => new { dimension = g.Key, total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    _ => (object)logs.GroupBy(l => "total").Select(g => new { dimension = "total", total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() })
                };
            }

            return Ok(new { success = true, data = result, meta = new { group_by, metric } });
        }
    }
}
