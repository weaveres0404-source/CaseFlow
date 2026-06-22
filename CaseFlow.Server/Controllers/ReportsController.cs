using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
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

        // GET /api/v1/reports/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.GetUserId();
            var role = User.GetRole();

            IQueryable<Case> casesQuery = _db.Cases.AsNoTracking();
            if (role == "PM")
            {
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId).ToListAsync();
                casesQuery = casesQuery.Where(c => myProjectIds.Contains(c.ProjectId));
            }
            else if (role == "SE")
            {
                var myCaseIds = await _db.CaseAssignments.AsNoTracking()
                    .Where(a => a.SeUserId == userId && a.IsActive)
                    .Select(a => a.CaseId).Distinct().ToListAsync();
                casesQuery = casesQuery.Where(c => myCaseIds.Contains(c.CaseId));
            }

            var statusCounts = await casesQuery
                .GroupBy(c => c.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            int Get(short s) => statusCounts.FirstOrDefault(x => x.status == s)?.count ?? 0;

            var now = TimeHelper.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            IQueryable<CaseLog> logsQuery = _db.CaseLogs.AsNoTracking();
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

            var thisMonthHours = await logsQuery
                .Where(l => l.CreatedAt >= monthStart)
                .SumAsync(l => (decimal?)l.HoursSpent) ?? 0m;

            var thisMonthCompleted = await casesQuery
                .Where(c => c.Status >= 40 && c.UpdatedAt >= monthStart)
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    status_summary = new
                    {
                        pending = Get(10),
                        assigned = Get(20),
                        in_progress = Get(30),
                        returned = Get(35),
                        completed = Get(40),
                        closed = Get(50),
                        cancelled = Get(60)
                    },
                    this_month = new
                    {
                        completed_cases = thisMonthCompleted,
                        total_hours = thisMonthHours
                    }
                }
            });
        }

        // POST /api/v1/reports/export
        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] ExportReportDto dto)
        {
            if (dto.DateFrom.HasValue && dto.DateTo.HasValue &&
                (dto.DateTo.Value - dto.DateFrom.Value).TotalDays > 366)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Date range must not exceed 1 year" } });

            var userId = User.GetUserId();
            var role = User.GetRole();

            var logsQuery = _db.CaseLogs.AsNoTracking()
                .Include(l => l.Case).ThenInclude(c => c.Project)
                .Include(l => l.Case).ThenInclude(c => c.Customer)
                .Include(l => l.Case).ThenInclude(c => c.Category)
                .Include(l => l.Case).ThenInclude(c => c.AssignedPm)
                .Include(l => l.HandlerUser)
                .AsQueryable();

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

            if (dto.ProjectId.HasValue) logsQuery = logsQuery.Where(l => l.Case.ProjectId == dto.ProjectId.Value);
            if (dto.CustomerId.HasValue) logsQuery = logsQuery.Where(l => l.Case.CustomerId == dto.CustomerId.Value);
            if (dto.DateFrom.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt >= dto.DateFrom.Value);
            if (dto.DateTo.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt <= dto.DateTo.Value);

            var logs = await logsQuery.OrderBy(l => l.Case.CaseNumber).ThenBy(l => l.LogDate).ToListAsync();

            IEnumerable<object> rows;
            if (dto.ReportType == "hours_gsheets")
            {
                rows = logs.Select(l => (object)new
                {
                    CaseNumber = l.Case.CaseNumber,
                    Customer = l.Case.Customer != null ? l.Case.Customer.CustomerName : "",
                    Project = l.Case.Project != null ? l.Case.Project.ProjectName : "",
                    Handler = l.HandlerUser != null ? l.HandlerUser.FullName : "",
                    LogDate = l.LogDate.ToString("yyyy-MM-dd"),
                    HoursSpent = l.HoursSpent,
                    Completed = l.StatusAfter >= 40 ? "Y" : ""
                });
            }
            else
            {
                rows = logs.Select(l => (object)new
                {
                    CaseNumber = l.Case.CaseNumber,
                    Customer = l.Case.Customer != null ? l.Case.Customer.CustomerName : "",
                    Project = l.Case.Project != null ? l.Case.Project.ProjectName : "",
                    Category = l.Case.Category != null ? l.Case.Category.CategoryName : "",
                    PM = l.Case.AssignedPm != null ? l.Case.AssignedPm.FullName : "",
                    Handler = l.HandlerUser != null ? l.HandlerUser.FullName : "",
                    LogDate = l.LogDate.ToString("yyyy-MM-dd"),
                    Method = l.HandlingMethod ?? "",
                    Result = l.HandlingResult ?? "",
                    HoursSpent = l.HoursSpent,
                    Headcount = l.Headcount,
                    StatusAfter = l.StatusAfter,
                    CreatedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });
            }

            var stream = new MemoryStream();
            await stream.SaveAsAsync(rows);
            stream.Position = 0;

            var filename = string.Format("CaseFlow_{0}_{1}.xlsx", dto.ReportType, TimeHelper.Now.ToString("yyyyMMdd_HHmmss"));
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        // GET /api/v1/reports/hours
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

            if (project_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.ProjectId == project_id.Value);
            if (customer_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.CustomerId == customer_id.Value);
            if (category_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.CategoryId == category_id.Value);
            if (status.HasValue) logsQuery = logsQuery.Where(l => l.Case.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(case_type)) logsQuery = logsQuery.Where(l => l.Case.CaseType == case_type);
            if (created_by.HasValue) logsQuery = logsQuery.Where(l => l.Case.CreatedBy == created_by.Value);
            if (assigned_pm_id.HasValue) logsQuery = logsQuery.Where(l => l.Case.AssignedPmId == assigned_pm_id.Value);
            if (handler_user_id.HasValue) logsQuery = logsQuery.Where(l => l.HandlerUserId == handler_user_id.Value);
            if (date_from.HasValue)
            {
                var from = DateOnly.FromDateTime(date_from.Value);
                logsQuery = logsQuery.Where(l => l.LogDate >= from);
            }
            if (date_to.HasValue)
            {
                var to = DateOnly.FromDateTime(date_to.Value);
                logsQuery = logsQuery.Where(l => l.LogDate <= to);
            }

            var logs = await logsQuery.ToListAsync();

            object result;

            if (metric == "count")
            {
                var cases = logs.Select(l => l.Case).DistinctBy(c => c.CaseId).ToList();
                result = group_by switch
                {
                    "status" => cases.GroupBy(c => c.Status).Select(g => new { dimension = g.Key.ToString(), label = g.Key.ToString(), value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    "project" => cases.GroupBy(c => c.Project.ProjectName).Select(g => new { dimension = g.Key, label = g.Key, value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    "customer" => cases.GroupBy(c => c.Customer.CustomerName).Select(g => new { dimension = g.Key, label = g.Key, value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    "category" => cases.GroupBy(c => c.Category.CategoryName).Select(g => new { dimension = g.Key, label = g.Key, value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    "created_by" => cases.GroupBy(c => c.CreatedByNavigation.FullName).Select(g => new { dimension = g.Key, label = g.Key, value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    "assigned_pm" => cases.Where(c => c.AssignedPm != null).GroupBy(c => c.AssignedPm!.FullName).Select(g => new { dimension = g.Key, label = g.Key, value = (decimal)g.Count(), count = g.Count() }).OrderBy(x => x.dimension),
                    _ => (object)new[] { new { dimension = "total", label = "total", value = (decimal)cases.Count, count = cases.Count } }
                };
            }
            else
            {
                result = group_by switch
                {
                    "se" => logs.GroupBy(l => l.HandlerUser.FullName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "project" => logs.GroupBy(l => l.Case.Project.ProjectName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "customer" => logs.GroupBy(l => l.Case.Customer.CustomerName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "category" => logs.GroupBy(l => l.Case.Category.CategoryName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "created_by" => logs.GroupBy(l => l.Case.CreatedByNavigation.FullName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    "assigned_pm" => logs.Where(l => l.Case.AssignedPm != null).GroupBy(l => l.Case.AssignedPm!.FullName).Select(g => new { dimension = g.Key, label = g.Key, value = g.Sum(l => l.HoursSpent), total_hours = g.Sum(l => l.HoursSpent), case_count = g.Select(l => l.CaseId).Distinct().Count() }).OrderBy(x => x.dimension),
                    _ => (object)new[] { new { dimension = "total", label = "total", value = logs.Sum(l => l.HoursSpent), total_hours = logs.Sum(l => l.HoursSpent), case_count = logs.Select(l => l.CaseId).Distinct().Count() } }
                };
            }

            return Ok(new { success = true, data = result, meta = new { group_by, metric } });
        }

        // GET /api/v1/reports/custom/suda-hours
        [HttpGet("custom/suda-hours")]
        public async Task<IActionResult> GetSudaHoursExport(
            [FromQuery] int project_id,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null)
        {
            if (project_id <= 0)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "project_id is required" } });

            var userId = User.GetUserId();
            var role = User.GetRole();
            var now = TimeHelper.Now;
            var from = date_from.HasValue
                ? DateOnly.FromDateTime(date_from.Value)
                : DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1));
            var toExclusive = date_to.HasValue
                ? DateOnly.FromDateTime(date_to.Value).AddDays(1)
                : from.AddMonths(1);

            var project = await _db.Projects.AsNoTracking()
                .Where(p => p.ProjectId == project_id)
                .Select(p => new { id = p.ProjectId, code = p.ProjectCode, name = p.ProjectName })
                .FirstOrDefaultAsync();

            if (project == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project not found" } });

            var logsQuery = _db.CaseLogs.AsNoTracking()
                .Include(l => l.Case)
                .Include(l => l.HandlerUser)
                .Where(l => l.Case.ProjectId == project_id)
                .Where(l => l.Case.Status != 60)
                .Where(l => l.LogDate >= from && l.LogDate < toExclusive)
                .AsQueryable();

            if (role == "PM")
            {
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();
                logsQuery = logsQuery.Where(l => myProjectIds.Contains(l.Case.ProjectId));
            }
            else if (role == "SE")
            {
                logsQuery = logsQuery.Where(l => l.HandlerUserId == userId);
            }

            var logs = await logsQuery
                .OrderBy(l => l.Case.CaseNumber)
                .ThenBy(l => l.LogDate)
                .ThenBy(l => l.CreatedAt)
                .ThenBy(l => l.LogId)
                .ToListAsync();

            var caseTypeCodes = logs.Select(l => l.Case.CaseType).Distinct().ToList();
            var caseTypeLabels = await _db.CaseTypes.AsNoTracking()
                .Where(t => caseTypeCodes.Contains(t.Code))
                .ToDictionaryAsync(t => t.Code, t => t.Label);

            string StatusLabel(short status) => status switch
            {
                10 => "立案",
                20 => "已派工",
                30 => "處理中",
                35 => "已退回",
                40 => "已完工",
                50 => "已完成",
                60 => "取消",
                _ => status.ToString()
            };

            var rows = logs
                .GroupBy(l => l.CaseId)
                .Select(g =>
                {
                    var orderedLogs = g.OrderBy(l => l.LogDate).ThenBy(l => l.CreatedAt).ThenBy(l => l.LogId).ToList();
                    var caseEntity = orderedLogs[0].Case;
                    var handlers = orderedLogs
                        .Select(l => l.HandlerUser.FullName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct()
                        .ToList();
                    var handlingMethods = orderedLogs
                        .Where(l => !string.IsNullOrWhiteSpace(l.HandlingMethod))
                        .Select(l => $"{l.HandlerUser.FullName}：{l.HandlingMethod.Trim()}")
                        .ToList();
                    var handlingResults = orderedLogs
                        .Where(l => !string.IsNullOrWhiteSpace(l.HandlingResult))
                        .Select(l => $"{l.HandlerUser.FullName}：{l.HandlingResult!.Trim()}")
                        .ToList();

                    return new
                    {
                        submitted_date = caseEntity.CreatedAt,
                        system = "客服",
                        response_content = caseEntity.Description,
                        owner_unit = "矩明",
                        status = StatusLabel(caseEntity.Status),
                        investigation_result = string.Join("\n", handlingMethods),
                        improvement_action = string.Join("\n", handlingResults),
                        hours_spent = orderedLogs.Sum(l => l.HoursSpent),
                        closed_date = caseEntity.ClosedAt,
                        case_number = caseEntity.CaseNumber,
                        request_no = "",
                        category = caseTypeLabels.GetValueOrDefault(caseEntity.CaseType, caseEntity.CaseType),
                        handlers = string.Join("\n", handlers)
                    };
                })
                .OrderBy(r => r.submitted_date)
                .ThenBy(r => r.case_number)
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    project,
                    period = new { date_from = from.ToString("yyyy-MM-dd"), date_to = toExclusive.AddDays(-1).ToString("yyyy-MM-dd") },
                    rows
                }
            });
        }
    }

    public class ExportReportDto
    {
        public string ReportType { get; set; } = "hours";
        public int? ProjectId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
