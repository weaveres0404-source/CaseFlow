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

            // 以下條件用於篩選「案件範圍」：找出符合條件（含日期區間內有任一筆處理紀錄）的案件，
            // 再將該案件的「所有處理紀錄」一併匯出，避免同案件的其它紀錄被截掉，
            // 使匯出總工時等於案件詳情頁的總工時（達成「畫面 = 匯出」一致）。
            if (dto.ProjectId.HasValue) logsQuery = logsQuery.Where(l => l.Case.ProjectId == dto.ProjectId.Value);
            if (dto.CustomerId.HasValue) logsQuery = logsQuery.Where(l => l.Case.CustomerId == dto.CustomerId.Value);
            if (dto.CategoryId.HasValue) logsQuery = logsQuery.Where(l => l.Case.CategoryId == dto.CategoryId.Value);
            if (dto.Status.HasValue) logsQuery = logsQuery.Where(l => l.Case.Status == dto.Status.Value);
            if (!string.IsNullOrWhiteSpace(dto.CaseType)) logsQuery = logsQuery.Where(l => l.Case.CaseType == dto.CaseType);
            if (dto.CreatedBy.HasValue) logsQuery = logsQuery.Where(l => l.Case.CreatedBy == dto.CreatedBy.Value);
            if (dto.AssignedPmId.HasValue) logsQuery = logsQuery.Where(l => l.Case.AssignedPmId == dto.AssignedPmId.Value);
            if (dto.HandlerUserId.HasValue) logsQuery = logsQuery.Where(l => l.HandlerUserId == dto.HandlerUserId.Value);

            // 日期區間先找出「命中的 CaseId 清單」
            var dateFilteredQuery = logsQuery;
            if (dto.DateFrom.HasValue)
            {
                var fromDt = dto.DateFrom.Value.Date;
                dateFilteredQuery = dateFilteredQuery.Where(l => (l.Case.OccurredAt ?? l.Case.CreatedAt) >= fromDt);
            }
            if (dto.DateTo.HasValue)
            {
                var toDt = dto.DateTo.Value.Date.AddDays(1);
                dateFilteredQuery = dateFilteredQuery.Where(l => (l.Case.OccurredAt ?? l.Case.CreatedAt) < toDt);
            }
            var matchedCaseIds = await dateFilteredQuery.Select(l => l.CaseId).Distinct().ToListAsync();

            // 再回頭將「命中案件」的所有處理紀錄拉出
            var logs = await logsQuery
                .Where(l => matchedCaseIds.Contains(l.CaseId))
                .OrderBy(l => l.Case.CaseNumber).ThenBy(l => l.LogDate)
                .ToListAsync();

            IEnumerable<object> rows;
            if (dto.ReportType == "overview")
            {
                // 總覽樣式：依所選維度彙總工時與案件數，最後附上「全部合計」列（客製化 Excel 樣式）
                var groupBy = string.IsNullOrWhiteSpace(dto.GroupBy) ? "category" : dto.GroupBy;
                var dimLabel = DimensionLabel(groupBy);
                Func<CaseLog, string> keySelector = groupBy switch
                {
                    "se" => l => l.HandlerUser?.FullName ?? "(未指定)",
                    "project" => l => l.Case.Project?.ProjectName ?? "(未指定)",
                    "customer" => l => l.Case.Customer?.CustomerName ?? "(未指定)",
                    "created_by" => l => l.Case.CreatedByNavigation?.FullName ?? "(未指定)",
                    "assigned_pm" => l => l.Case.AssignedPm?.FullName ?? "(未轉派)",
                    _ => l => l.Case.Category?.CategoryName ?? "(未分類)"
                };

                var grouped = logs.GroupBy(keySelector)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Hours = g.Sum(x => x.HoursSpent),
                        Cases = g.Select(x => x.CaseId).Distinct().Count()
                    })
                    .OrderBy(x => x.Key)
                    .ToList();

                var overviewRows = new List<Dictionary<string, object>>();
                foreach (var g in grouped)
                {
                    overviewRows.Add(new Dictionary<string, object>
                    {
                        [dimLabel] = g.Key,
                        ["工時合計(hr)"] = g.Hours,
                        ["案件數"] = g.Cases
                    });
                }
                overviewRows.Add(new Dictionary<string, object>
                {
                    [dimLabel] = "全部合計",
                    ["工時合計(hr)"] = grouped.Sum(x => x.Hours),
                    ["案件數"] = logs.Select(l => l.CaseId).Distinct().Count()
                });
                rows = overviewRows;
            }
            else if (dto.ReportType == "hours_gsheets")
            {
                rows = logs.Select(l => (object)new
                {
                    CaseNumber = l.Case.CaseNumber,
                    Customer = l.Case.Customer != null ? l.Case.Customer.CustomerName : "",
                    Project = l.Case.Project != null ? l.Case.Project.ProjectName : "",
                    Handler = l.HandlerUser != null ? l.HandlerUser.FullName : "",
                    LogDate = l.LogDate.ToString("yyyy-MM-dd"),
                    OccurredDate = (l.Case.OccurredAt ?? l.Case.CreatedAt).ToString("yyyy-MM-dd"),
                    SubmittedDate = l.Case.CreatedAt.ToString("yyyy-MM-dd"),
                    ClosedDate = l.Case.ClosedAt.HasValue ? l.Case.ClosedAt.Value.ToString("yyyy-MM-dd") : "",
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
                    OccurredDate = (l.Case.OccurredAt ?? l.Case.CreatedAt).ToString("yyyy-MM-dd"),
                    SubmittedDate = l.Case.CreatedAt.ToString("yyyy-MM-dd"),
                    ClosedDate = l.Case.ClosedAt.HasValue ? l.Case.ClosedAt.Value.ToString("yyyy-MM-dd") : "",
                    Method = l.HandlingMethod ?? "",
                    Result = l.HandlingResult ?? "",
                    HoursSpent = l.HoursSpent,
                    Headcount = l.Headcount,
                    StatusAfter = l.StatusAfter
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

            // Date range matches cases by Case.OccurredAt (fallback CreatedAt), not CaseLog.LogDate,
            // then pulls ALL of that case's logs so totals match the case detail page (consistent
            // with /reports/export and /reports/custom/suda-hours).
            var dateFilteredQuery = logsQuery;
            if (date_from.HasValue)
            {
                var fromDt = date_from.Value.Date;
                dateFilteredQuery = dateFilteredQuery.Where(l => (l.Case.OccurredAt ?? l.Case.CreatedAt) >= fromDt);
            }
            if (date_to.HasValue)
            {
                var toDt = date_to.Value.Date.AddDays(1);
                dateFilteredQuery = dateFilteredQuery.Where(l => (l.Case.OccurredAt ?? l.Case.CreatedAt) < toDt);
            }
            var matchedCaseIds = await dateFilteredQuery.Select(l => l.CaseId).Distinct().ToListAsync();

            var logs = await logsQuery.Where(l => matchedCaseIds.Contains(l.CaseId)).ToListAsync();

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
            [FromQuery] DateTime? date_to = null,
            [FromQuery] string format = "json")
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

            // 案件範圍 (含權限篩選)
            var baseQuery = _db.CaseLogs.AsNoTracking()
                .Include(l => l.Case)
                .Include(l => l.HandlerUser)
                .Where(l => l.Case.ProjectId == project_id)
                .Where(l => l.Case.Status != 60)
                .AsQueryable();

            if (role == "PM")
            {
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();
                baseQuery = baseQuery.Where(l => myProjectIds.Contains(l.Case.ProjectId));
            }
            else if (role == "SE")
            {
                baseQuery = baseQuery.Where(l => l.HandlerUserId == userId);
            }

            // 日期區間先找出「命中的 CaseId」，再撈出該案件所有 log（使工時 = 詳情頁工時）
            // Date range now matches on Case.OccurredAt (fallback CreatedAt) instead of
            // CaseLog.LogDate, per customer requirement to bucket cases by occurred/submission date.
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            var toExclusiveDt = toExclusive.ToDateTime(TimeOnly.MinValue);
            var matchedCaseIds = await baseQuery
                .Where(l => (l.Case.OccurredAt ?? l.Case.CreatedAt) >= fromDt
                    && (l.Case.OccurredAt ?? l.Case.CreatedAt) < toExclusiveDt)
                .Select(l => l.CaseId)
                .Distinct()
                .ToListAsync();

            var logs = await baseQuery
                .Where(l => matchedCaseIds.Contains(l.CaseId))
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
                        submitted_date = caseEntity.CreatedAt.ToString("yyyy-MM-dd"),
                        occurred_date = caseEntity.OccurredAt.HasValue ? caseEntity.OccurredAt.Value.ToString("yyyy-MM-dd") : "",
                        system = "客服",
                        response_content = caseEntity.Description,
                        owner_unit = "矩明",
                        status = StatusLabel(caseEntity.Status),
                        investigation_result = string.Join("\n", handlingMethods),
                        improvement_action = string.Join("\n", handlingResults),
                        hours_spent = orderedLogs.Sum(l => l.HoursSpent),
                        closed_date = caseEntity.ClosedAt.HasValue ? caseEntity.ClosedAt.Value.ToString("yyyy-MM-dd") : "",
                        case_number = caseEntity.CaseNumber,
                        request_no = "",
                        category = caseTypeLabels.GetValueOrDefault(caseEntity.CaseType, caseEntity.CaseType),
                        handlers = string.Join("\n", handlers)
                    };
                })
                .OrderBy(r => r.submitted_date)
                .ThenBy(r => r.case_number)
                .ToList();

            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                var xlsxRows = rows.Select(r => (object)new Dictionary<string, object>
                {
                    ["提報日期"] = r.submitted_date,
                    ["補件日期"] = r.occurred_date,
                    ["系統"] = r.system,
                    ["問題內容"] = r.response_content,
                    ["負責單位"] = r.owner_unit,
                    ["狀態"] = r.status,
                    ["調查結果"] = r.investigation_result,
                    ["改善對策"] = r.improvement_action,
                    ["工時"] = r.hours_spent,
                    ["結案日期"] = r.closed_date,
                    ["案件編號"] = r.case_number,
                    ["需求單號"] = r.request_no,
                    ["分類"] = r.category,
                    ["處理人員"] = r.handlers
                }
                ).ToList();

                var xlsxStream = new MemoryStream();
                await xlsxStream.SaveAsAsync(xlsxRows);
                xlsxStream.Position = 0;
                var xlsxFilename = string.Format("SudaHours_{0}_{1}.xlsx", project.code, TimeHelper.Now.ToString("yyyyMMdd_HHmmss"));
                return File(xlsxStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsxFilename);
            }

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

        private static string DimensionLabel(string? groupBy) => groupBy switch
        {
            "se" => "工程師",
            "project" => "專案",
            "customer" => "客戶",
            "category" => "問題分類",
            "created_by" => "立案者",
            "assigned_pm" => "轉派 PM",
            _ => "問題分類"
        };
    }

    public class ExportReportDto
    {
        public string ReportType { get; set; } = "hours";
        public string? GroupBy { get; set; }
        public string? Metric { get; set; }
        public int? ProjectId { get; set; }
        public int? CustomerId { get; set; }
        public int? CategoryId { get; set; }
        public short? Status { get; set; }
        public string? CaseType { get; set; }
        public int? CreatedBy { get; set; }
        public int? AssignedPmId { get; set; }
        public int? HandlerUserId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
