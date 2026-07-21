using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/cases")]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;
        private readonly ILogger<CasesController> _logger;

        public CasesController(CaseFlowDbContext db, ILogger<CasesController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        private Task<bool> IsProjectPmAsync(int userId, int projectId)
        {
            if (User.GetRole() == "SysAdmin") return Task.FromResult(true);
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == "PM");
        }

        private Task<bool> IsProjectMemberRoleAsync(int userId, int projectId, string role)
        {
            if (User.GetRole() == "SysAdmin") return Task.FromResult(true);
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == role);
        }

        // GET /api/v1/cases — 案件列表 (含分頁、篩選、搜尋)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int page_size = 20,
            [FromQuery] string? q = null,
            [FromQuery] int? project_id = null,
            [FromQuery] int? customer_id = null,
            [FromQuery] short? status = null,
            [FromQuery] string? case_type = null,
            [FromQuery] int? category_id = null,
            [FromQuery] string? priority = null,
            [FromQuery] int? created_by = null,
            [FromQuery] int? assigned_pm_id = null,
            [FromQuery] int? se_user_id = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null,
            [FromQuery] string? sort = null,
            [FromQuery] bool assigned_to_me = false,
            [FromQuery] bool created_by_me = false,
            [FromQuery] bool open_only = false,
            [FromQuery] int? created_by_id = null)
        {
            try
            {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var userId = User.GetUserId();
            var role = User.GetRole();

            if (created_by_id.HasValue && !created_by.HasValue)
                created_by = created_by_id;

            var query = _db.Cases.AsNoTracking()
                .Include(c => c.Project)
                .Include(c => c.Customer)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.AssignedPm)
                .Include(c => c.CaseAssignments.Where(a => a.IsActive))
                    .ThenInclude(a => a.SeUser)
                .AsQueryable();
            List<int>? assignedCaseIdsForCurrentUser = null;

            // 權限過濾
            if (role == "PM")
            {
                // PM 可看到：自己擔任 PM 的專案案件，或自己被指派為 SE 的案件（跨專案兼任）
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId).ToListAsync();
                assignedCaseIdsForCurrentUser = await _db.CaseAssignments.AsNoTracking()
                    .Where(a => a.SeUserId == userId && a.IsActive)
                    .Select(a => a.CaseId).Distinct().ToListAsync();
                query = query.Where(c =>
                    myProjectIds.Contains(c.ProjectId) ||
                    assignedCaseIdsForCurrentUser.Contains(c.CaseId));
            }
            else if (role == "SE")
            {
                assignedCaseIdsForCurrentUser = await _db.CaseAssignments.AsNoTracking()
                    .Where(a => a.SeUserId == userId && a.IsActive)
                    .Select(a => a.CaseId).Distinct().ToListAsync();
                query = query.Where(c => assignedCaseIdsForCurrentUser.Contains(c.CaseId));
            }

            // 篩選條件
            if (project_id.HasValue) query = query.Where(c => c.ProjectId == project_id.Value);
            if (customer_id.HasValue) query = query.Where(c => c.CustomerId == customer_id.Value);
            if (status.HasValue) query = query.Where(c => c.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(case_type)) query = query.Where(c => c.CaseType == case_type);
            if (category_id.HasValue) query = query.Where(c => c.CategoryId == category_id.Value);
            if (!string.IsNullOrWhiteSpace(priority)) query = query.Where(c => c.Priority == priority);
            if (created_by.HasValue) query = query.Where(c => c.CreatedBy == created_by.Value);
            if (assigned_pm_id.HasValue) query = query.Where(c => c.AssignedPmId == assigned_pm_id.Value);
            if (se_user_id.HasValue)
                query = query.Where(c => c.CaseAssignments.Any(a => a.SeUserId == se_user_id.Value && a.IsActive));
            if (date_from.HasValue) query = query.Where(c => c.CreatedAt >= date_from.Value);
            if (date_to.HasValue) query = query.Where(c => c.CreatedAt <= date_to.Value);

            if (assigned_to_me)
            {
                assignedCaseIdsForCurrentUser ??= await _db.CaseAssignments.AsNoTracking()
                    .Where(a => a.SeUserId == userId && a.IsActive)
                    .Select(a => a.CaseId).Distinct().ToListAsync();
                query = query.Where(c => assignedCaseIdsForCurrentUser.Contains(c.CaseId));
            }
            if (created_by_me)
                query = query.Where(c => c.CreatedBy == userId);
            if (open_only)
                query = query.Where(c => c.Status != 50 && c.Status != 60);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.CaseNumber, $"%{q}%") ||
                    EF.Functions.ILike(c.ReporterName, $"%{q}%") ||
                    EF.Functions.ILike(c.Description, $"%{q}%"));
            }

            // 排序
            query = sort switch
            {
                "created_at,asc" => query.OrderBy(c => c.CreatedAt),
                "status,asc" => query.OrderBy(c => c.Status),
                "status,desc" => query.OrderByDescending(c => c.Status),
                "priority,asc" => query.OrderBy(c => c.Priority),
                "priority,desc" => query.OrderByDescending(c => c.Priority),
                _ => query.OrderByDescending(c => c.UpdatedAt)
            };

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(c => new
                {
                    id = c.CaseId,
                    case_number = c.CaseNumber,
                    project = new { id = c.Project.ProjectId, code = c.Project.ProjectCode, name = c.Project.ProjectName },
                    customer = new { id = c.Customer.CustomerId, name = c.Customer.CustomerName },
                    case_type = c.CaseType,
                    priority = c.Priority,
                    status = c.Status,
                    created_at = c.CreatedAt,
                    created_by = new { id = c.CreatedByNavigation.UserId, full_name = c.CreatedByNavigation.FullName },
                    assigned_pm = c.AssignedPm != null ? new { id = c.AssignedPm.UserId, full_name = c.AssignedPm.FullName } : null,
                    assigned_ses = c.CaseAssignments.Where(a => a.IsActive).Select(a => new { id = a.SeUser.UserId, full_name = a.SeUser.FullName }),
                    updated_at = c.UpdatedAt,
                    _caseId = c.CaseId   // used below for slug
                })
                .ToListAsync();

            var itemsWithSlug = items.Select(c => new
            {
                c.id,
                short_id = SlugHelper.Encode(c._caseId),
                c.case_number,
                c.project,
                c.customer,
                c.case_type,
                c.priority,
                c.status,
                c.created_at,
                c.created_by,
                c.assigned_pm,
                c.assigned_ses,
                c.updated_at
            });

            return Ok(new { success = true, data = itemsWithSlug, meta = new { page, page_size, total } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GetAll cases failed | {ExType}: {Message}",
                    ex.GetType().FullName, ex.Message);
                Console.Error.WriteLine($"[CASES/GETALL] {ex}");
                throw; // ExceptionLoggingMiddleware + UseExceptionHandler will return HTTP 500
            }
        }

        // GET /api/v1/cases/:slug — 案件詳情 (胖 Payload)
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetById(string slug)
        {
            var id = SlugHelper.Decode(slug);
            if (id == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });
            var c = await _db.Cases.AsNoTracking().AsSplitQuery()
                .Include(x => x.Project)
                .Include(x => x.Customer)
                .Include(x => x.Category)
                .Include(x => x.Module)
                .Include(x => x.CreatedByNavigation)
                .Include(x => x.AssignedPm)
                .Include(x => x.ClosedByNavigation)
                .Include(x => x.CancelledByNavigation)
                .Include(x => x.RelatedCase)
                .Include(x => x.CaseAssignments).ThenInclude(a => a.SeUser)
                .Include(x => x.CaseAssignments).ThenInclude(a => a.AssignedByNavigation)
                .Include(x => x.CaseLogs).ThenInclude(l => l.HandlerUser)
                .Include(x => x.CaseLogs).ThenInclude(l => l.RefCase)
                .Include(x => x.CaseEstimations).ThenInclude(e => e.EstimatorUser)
                .Include(x => x.CaseReplies).ThenInclude(r => r.ReplierUser)
                .FirstOrDefaultAsync(x => x.CaseId == id.Value);

            if (c == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 單筆案件的角色存取控制
            var userId = User.GetUserId();
            var role   = User.GetRole();

            bool allowedView = role == "SysAdmin";
            if (!allowedView && role == "SE")
            {
                allowedView = c.CaseAssignments.Any(a => a.SeUserId == userId && a.IsActive);
            }
            else if (!allowedView && role == "PM")
            {
                // 允許：(1) assigned_pm_id (2) created_by (3) 該專案的 ProjectMember(PM)
                // (4) 「目前」有效的 SE 指派（PM 在其他專案可能兼任 SE）
                allowedView = c.AssignedPmId == userId || c.CreatedBy == userId;
                if (!allowedView)
                {
                    allowedView = await _db.ProjectMembers.AsNoTracking()
                        .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == c.ProjectId && pm.IsActive && pm.MemberRole == "PM");
                }
                if (!allowedView)
                {
                    allowedView = c.CaseAssignments.Any(a => a.SeUserId == userId && a.IsActive);
                }
            }

            if (!allowedView)
            {
                // 若使用者曾有指派或曾收到該案件通知，回傳特殊代碼讓前端顯示「已取消轉派」訊息
                bool hadAccess = c.CaseAssignments.Any(a => a.SeUserId == userId)
                    || await _db.Notifications.AsNoTracking()
                        .AnyAsync(n => n.RecipientUserId == userId && n.CaseId == c.CaseId);
                if (hadAccess)
                    return StatusCode(403, new { success = false, error = new { code = "ACCESS_REVOKED", message = "該案件已取消轉派給您" } });
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "您沒有權限查看此案件" } });
            }

            // 附件需另查 (多態關聯)
            var attachments = await _db.Attachments.AsNoTracking()
                .Include(a => a.UploadedByNavigation)
                .Where(a => (a.EntityType == "case" && a.EntityId == id) ||
                            (a.EntityType == "case_log" && c.CaseLogs.Select(l => l.LogId).Contains(a.EntityId)) ||
                            (a.EntityType == "case_estimation" && c.CaseEstimations.Select(e => e.EstimationId).Contains(a.EntityId)) ||
                            (a.EntityType == "case_reply" && c.CaseReplies.Select(r => r.ReplyId).Contains(a.EntityId)))
                .Select(a => new
                {
                    id = a.AttachmentId,
                    file_name = a.FileName,
                    file_size = a.FileSize,
                    mime_type = a.MimeType,
                    entity_type = a.EntityType,
                    entity_id = a.EntityId,
                    uploaded_by = new { id = a.UploadedByNavigation.UserId, full_name = a.UploadedByNavigation.FullName },
                    uploaded_at = a.UploadedAt
                })
                .ToListAsync();

            var result = new
            {
                id = c.CaseId,
                short_id = SlugHelper.Encode(c.CaseId),
                case_number = c.CaseNumber,
                project = new { id = c.Project.ProjectId, code = c.Project.ProjectCode, name = c.Project.ProjectName },
                customer = new { id = c.Customer.CustomerId, name = c.Customer.CustomerName },
                category = new { id = c.Category.CategoryId, name = c.Category.CategoryName },
                module = c.Module != null ? new { id = c.Module.ModuleId, name = c.Module.ModuleName } : null,
                reporter_name = c.ReporterName,
                reporter_phone = c.ReporterPhone,
                reporter_email = c.ReporterEmail,
                case_type = c.CaseType,
                priority = c.Priority,
                description = c.Description,
                status = c.Status,
                created_by = new { id = c.CreatedByNavigation.UserId, full_name = c.CreatedByNavigation.FullName },
                assigned_pm = c.AssignedPm != null ? new { id = c.AssignedPm.UserId, full_name = c.AssignedPm.FullName } : null,
                closed_by = c.ClosedByNavigation != null ? new { id = c.ClosedByNavigation.UserId, full_name = c.ClosedByNavigation.FullName } : null,
                cancelled_by = c.CancelledByNavigation != null ? new { id = c.CancelledByNavigation.UserId, full_name = c.CancelledByNavigation.FullName } : null,
                related_case = c.RelatedCase != null ? new { id = c.RelatedCase.CaseId, short_id = SlugHelper.Encode(c.RelatedCase.CaseId), case_number = c.RelatedCase.CaseNumber } : null,
                relation_type = c.RelationType,
                closed_at = c.ClosedAt,
                cancelled_at = c.CancelledAt,
                created_at = c.CreatedAt,
                updated_at = c.UpdatedAt,
                assignments = c.CaseAssignments.OrderByDescending(a => a.AssignedAt).Select(a => new
                {
                    id = a.AssignmentId,
                    se = new { id = a.SeUser.UserId, full_name = a.SeUser.FullName },
                    assigned_by = new { id = a.AssignedByNavigation.UserId, full_name = a.AssignedByNavigation.FullName },
                    is_primary = a.IsPrimary,
                    instructions = a.Instructions,
                    expected_completion_date = a.ExpectedCompletionDate,
                    is_active = a.IsActive,
                    assigned_at = a.AssignedAt
                }),
                logs = c.CaseLogs.OrderByDescending(l => l.LogDate).ThenByDescending(l => l.CreatedAt).Select(l => new
                {
                    id = l.LogId,
                    log_date = l.LogDate,
                    handler = new { id = l.HandlerUser.UserId, full_name = l.HandlerUser.FullName },
                    handling_method = l.HandlingMethod,
                    handling_result = l.HandlingResult,
                    hours_spent = l.HoursSpent,
                    headcount = l.Headcount,
                    status_after = l.StatusAfter,
                    created_at = l.CreatedAt,
                    ref_case = l.RefCase == null ? null : new { id = l.RefCase.CaseId, short_id = SlugHelper.Encode(l.RefCase.CaseId), case_number = l.RefCase.CaseNumber }
                }),
                estimations = c.CaseEstimations.OrderBy(e => e.SeqNo).Select(e => new
                {
                    id = e.EstimationId,
                    seq_no = e.SeqNo,
                    request_date = e.RequestDate,
                    summary = e.Summary,
                    estimated_hours = e.EstimatedHours,
                    reply_date = e.ReplyDate,
                    estimation_status = e.EstimationStatus,
                    estimator = new { id = e.EstimatorUser.UserId, full_name = e.EstimatorUser.FullName },
                    remarks = e.Remarks,
                    created_at = e.CreatedAt
                }),
                replies = c.CaseReplies.OrderByDescending(r => r.ReplyDate).ThenByDescending(r => r.CreatedAt).Select(r => new
                {
                    id = r.ReplyId,
                    reply_date = r.ReplyDate,
                    replier = new { id = r.ReplierUser.UserId, full_name = r.ReplierUser.FullName },
                    reply_content = r.ReplyContent,
                    created_at = r.CreatedAt
                }),
                attachments,
                summary = new
                {
                    total_hours = c.CaseLogs.Sum(l => l.HoursSpent),
                    total_headcount = c.CaseLogs.Sum(l => l.Headcount),
                    log_count = c.CaseLogs.Count,
                    estimation_count = c.CaseEstimations.Count,
                    reply_count = c.CaseReplies.Count
                }
            };

            return Ok(new { success = true, data = result });
        }

        // POST /api/v1/cases — 建立案件
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CaseCreateDto dto)
        {
            try
            {
                if (dto.ProjectId <= 0 || dto.CategoryId <= 0 || string.IsNullOrWhiteSpace(dto.ReporterName) || string.IsNullOrWhiteSpace(dto.Description))
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "project_id, category_id, reporter_name and description are required" } });

                var userId = User.GetUserId();
                var caseTypeCode = string.IsNullOrWhiteSpace(dto.CaseType) ? "REQUEST" : dto.CaseType.Trim();
                var priorityCode = string.IsNullOrWhiteSpace(dto.Priority) ? "MEDIUM" : dto.Priority.Trim();

                // 專案與編號前綴
                var project = await _db.Projects.AsNoTracking()
                    .Include(p => p.ProjectCodeRef)
                    .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId && p.IsActive);
                if (project == null)
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid project_id" } });

                // 專案所屬客戶由後端決定，避免前端帶錯值造成 FK 或資料錯置
                var customerId = project.CustomerId;

                var caseType = await _db.CaseTypes.AsNoTracking()
                    .Include(t => t.ProjectLinks)
                    .FirstOrDefaultAsync(t => t.Code == caseTypeCode && t.IsActive);
                if (caseType == null)
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = $"Invalid case_type: {caseTypeCode}" } });
                if (caseType.ProjectLinks.Any() && !caseType.ProjectLinks.Any(l => l.ProjectId == dto.ProjectId))
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Selected case type is not allowed for this project" } });

                var category = await _db.ProblemCategories.AsNoTracking()
                    .Include(c => c.ProjectLinks)
                    .Include(c => c.CaseTypeLinks)
                    .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);
                if (category == null)
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid category_id" } });
                if (category.ProjectLinks.Any() && !category.ProjectLinks.Any(l => l.ProjectId == dto.ProjectId))
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Selected category is not available for this project" } });
                if (category.CaseTypeLinks.Any() && !category.CaseTypeLinks.Any(l => l.TypeId == caseType.TypeId))
                    return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Selected category does not match the case type" } });

                if (dto.ModuleId.HasValue)
                {
                    var moduleExists = await _db.SystemModules.AsNoTracking()
                        .AnyAsync(m => m.ModuleId == dto.ModuleId.Value && m.ProjectId == dto.ProjectId && m.IsActive);
                    if (!moduleExists)
                        return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid module_id for this project" } });
                }

                var yearMonth = TimeHelper.TaipeiNow.ToString("yyyyMM");
                var codePrefix = project.ProjectCodeRef?.Code ?? project.ProjectCode;
                var prefix = $"{codePrefix}-{yearMonth}-";
                var lastCase = await _db.Cases.AsNoTracking()
                    .Where(c => c.CaseNumber.StartsWith(prefix))
                    .OrderByDescending(c => c.CaseNumber)
                    .FirstOrDefaultAsync();

                int seq = 1;
                if (lastCase != null)
                {
                    var parts = lastCase.CaseNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[^1], out int lastSeq))
                        seq = lastSeq + 1;
                }

                var caseNumber = $"{prefix}{seq:D3}";
                var now = TimeHelper.Now;
                var entity = new Case
                {
                    CaseNumber = caseNumber,
                    ProjectId = dto.ProjectId,
                    CustomerId = customerId,
                    CategoryId = dto.CategoryId,
                    ModuleId = dto.ModuleId,
                    ReporterName = dto.ReporterName.Trim(),
                    ReporterPhone = dto.ReporterPhone,
                    ReporterEmail = dto.ReporterEmail,
                    CaseType = caseTypeCode,
                    Priority = priorityCode,
                    Description = dto.Description,
                    Status = 10,
                    CreatedBy = userId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedCaseId = dto.RelatedCaseId,
                    RelationType = dto.RelatedCaseId.HasValue ? "REOPEN" : null
                };

                _db.Cases.Add(entity);

                var projectPms = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.ProjectId == dto.ProjectId && pm.MemberRole == "PM" && pm.IsActive)
                    .Select(pm => pm.UserId)
                    .ToListAsync();

                foreach (var pmId in projectPms)
                {
                    _db.Notifications.Add(new Notification
                    {
                        RecipientUserId = pmId,
                        NotificationType = "CASE_CREATED",
                        Title = $"新案件 {caseNumber}",
                        Message = $"新案件已建立：{dto.Description?.Substring(0, Math.Min(60, dto.Description?.Length ?? 0))}",
                        IsRead = false,
                        CreatedAt = now
                    });
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = "CASE_CREATED",
                    EntityType = "case",
                    CreatedAt = now
                });

                await _db.SaveChangesAsync();

                var auditLog = await _db.AuditLogs.OrderByDescending(a => a.AuditId).FirstOrDefaultAsync(a => a.UserId == userId && a.Action == "CASE_CREATED");
                if (auditLog != null)
                {
                    auditLog.CaseId = entity.CaseId;
                    auditLog.EntityId = entity.CaseId;
                    await _db.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetById), new { slug = SlugHelper.Encode(entity.CaseId) },
                    new { success = true, data = new { id = entity.CaseId, short_id = SlugHelper.Encode(entity.CaseId), case_number = caseNumber } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Create case failed | ProjectId={ProjectId} CategoryId={CategoryId} CaseType={CaseType} UserId={UserId}",
                    dto.ProjectId, dto.CategoryId, dto.CaseType, User.GetUserId());
                Console.Error.WriteLine($"[CASES/CREATE] {ex}");
                throw;
            }
        }

        // PATCH /api/v1/cases/:id — 修改案件基本欄位
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CaseUpdateDto dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status == 50 || entity.Status == 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot edit a closed or cancelled case" } });

            if (dto.CategoryId.HasValue) entity.CategoryId = dto.CategoryId.Value;
            if (dto.ModuleId.HasValue) entity.ModuleId = dto.ModuleId.Value;
            if (!string.IsNullOrWhiteSpace(dto.ReporterName)) entity.ReporterName = dto.ReporterName;
            if (dto.ReporterPhone != null) entity.ReporterPhone = dto.ReporterPhone;
            if (dto.ReporterEmail != null) entity.ReporterEmail = dto.ReporterEmail;
            if (!string.IsNullOrWhiteSpace(dto.CaseType)) entity.CaseType = dto.CaseType;
            if (!string.IsNullOrWhiteSpace(dto.Priority)) entity.Priority = dto.Priority;
            if (!string.IsNullOrWhiteSpace(dto.Description)) entity.Description = dto.Description;
            entity.UpdatedAt = TimeHelper.Now;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = entity.CaseId } });
        }

        #region RPC 狀態流轉

        // POST /api/v1/cases/:id/assign — 派工
        [HttpPost("{id:int}/assign")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignDto dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 允許來源：10(待處理)、20(已派工)、30(處理中)、35(已退回)
            if (entity.Status == 50 || entity.Status == 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot assign from current status", details = new { current_status = entity.Status } } });

            var userId = User.GetUserId();
            if (!await IsProjectPmAsync(userId, entity.ProjectId))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以轉派案件" } });

            var now = TimeHelper.Now;

            // 將既有 active assignments 設為 inactive
            var existingAssignments = await _db.CaseAssignments.Where(a => a.CaseId == id && a.IsActive).ToListAsync();
            foreach (var a in existingAssignments) a.IsActive = false;

            // 建立新 assignments
            foreach (var seId in dto.SeUserIds)
            {
                _db.CaseAssignments.Add(new CaseAssignment
                {
                    CaseId = id,
                    SeUserId = seId,
                    AssignedBy = userId,
                    IsPrimary = seId == dto.PrimarySeUserId,
                    Instructions = dto.Instructions,
                    ExpectedCompletionDate = dto.ExpectedCompletionDate,
                    IsActive = true,
                    AssignedAt = now,
                    CreatedAt = now
                });

                // 通知 SE
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = seId,
                    CaseId = id,
                    NotificationType = "CASE_ASSIGNED",
                    Title = $"案件派工 {entity.CaseNumber}",
                    Message = "您有新的案件待處理",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            // 僅從 status=10 才推進到 20；改派時維持現狀（不回推）
            if (entity.Status == 10) entity.Status = 20;
            entity.AssignedPmId = userId;
            entity.UpdatedAt = now;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                CaseId = id,
                Action = "CASE_ASSIGNED",
                EntityType = "case",
                EntityId = id,
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id, status = entity.Status } });
        }

        // POST /api/v1/cases/:id/complete
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var entity = await _db.Cases
                .Include(x => x.CaseLogs)
                .FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            var hasReplyHistory = entity.CaseLogs.Any(log => log.StatusAfter == 30);
            var canComplete = entity.Status == 30
                || entity.Status == 35
                || (entity.Status == 20 && hasReplyHistory);

            if (!canComplete)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot complete from current status", details = new { current_status = entity.Status } } });

            var completeUserId = User.GetUserId();
            if (!await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM"))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以回報完工" } });

            var now = TimeHelper.Now;
            entity.Status = 40;
            entity.UpdatedAt = now;

            // 通知轉派 PM
            if (entity.AssignedPmId.HasValue)
            {
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = entity.AssignedPmId.Value,
                    CaseId = id,
                    NotificationType = "WORK_COMPLETED",
                    Title = $"案件完工 {entity.CaseNumber}",
                    Message = "案件已完工，請確認是否結案",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "WORK_COMPLETED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, short_id = SlugHelper.Encode(id), status = 40 } });
        }

        // POST /api/v1/cases/:id/return — 退回
        [HttpPost("{id:int}/return")]
        public async Task<IActionResult> Return(int id, [FromBody] ReturnDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reason is required" } });

            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot return from current status", details = new { current_status = entity.Status } } });

            if (!await IsProjectPmAsync(User.GetUserId(), entity.ProjectId))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以退回案件" } });

            var now = TimeHelper.Now;
            entity.Status = 35;
            entity.UpdatedAt = now;

            // 通知相關 SE
            var seIds = await _db.CaseAssignments.Where(a => a.CaseId == id && a.IsActive).Select(a => a.SeUserId).ToListAsync();
            foreach (var seId in seIds)
            {
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = seId,
                    CaseId = id,
                    NotificationType = "CASE_RETURNED",
                    Title = $"案件退回 {entity.CaseNumber}",
                    Message = dto?.Reason ?? "案件被退回，請重新處理",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "CASE_RETURNED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, short_id = SlugHelper.Encode(id), status = 35 } });
        }

        // POST /api/v1/cases/:id/close — 結案
        [HttpPost("{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 權限：限該專案 ProjectMember 角色為 PM。
            var actorId = User.GetUserId();
            if (!await IsProjectPmAsync(actorId, entity.ProjectId))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以確認結案" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot close from current status", details = new { current_status = entity.Status } } });

            var now = TimeHelper.Now;
            entity.Status = 50;
            entity.ClosedBy = User.GetUserId();
            entity.ClosedAt = now;
            entity.UpdatedAt = now;

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "CASE_CLOSED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, short_id = SlugHelper.Encode(id), status = 50 } });
        }

        // POST /api/v1/cases/:id/cancel — 取消
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)
        {

            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            var allowed = new short[] { 10, 20, 30, 35, 40 };
            if (!allowed.Contains(entity.Status))
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot cancel from current status", details = new { current_status = entity.Status } } });

            var actorId = User.GetUserId();
            var canCancel = await IsProjectPmAsync(actorId, entity.ProjectId);
            if (!canCancel)
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以取消案件" } });

            var now = TimeHelper.Now;
            entity.Status = 60;
            entity.CancelledBy = User.GetUserId();
            entity.CancelledAt = now;
            entity.UpdatedAt = now;

            // 通知相關 SE + PM
            var seIds = await _db.CaseAssignments.Where(a => a.CaseId == id && a.IsActive).Select(a => a.SeUserId).ToListAsync();
            var notifyIds = new HashSet<int>(seIds);
            if (entity.AssignedPmId.HasValue) notifyIds.Add(entity.AssignedPmId.Value);
            notifyIds.Add(entity.CreatedBy);

            foreach (var uid in notifyIds)
            {
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = uid,
                    CaseId = id,
                    NotificationType = "CASE_CANCELLED",
                    Title = $"案件取消 {entity.CaseNumber}",
                    Message = dto?.Reason ?? "案件已被取消",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "CASE_CANCELLED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, short_id = SlugHelper.Encode(id), status = 60 } });
        }

        // POST /api/v1/cases/:id/reopen — 建新案件
        [HttpPost("{id:int}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var original = await _db.Cases.AsNoTracking().FirstOrDefaultAsync(x => x.CaseId == id);
            if (original == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 權限：限該案件的 PM（assigned_pm 或該專案的 PM ProjectMember）或 SysAdmin
            var actorId = User.GetUserId();
            var actorRole = User.GetRole();
            if (actorRole != "SysAdmin")
            {
                if (actorRole != "PM")
                    return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該案件的 PM 可以重開案件" } });
                bool isProjectPm = original.AssignedPmId == actorId
                    || await _db.ProjectMembers.AsNoTracking()
                        .AnyAsync(pm => pm.UserId == actorId && pm.ProjectId == original.ProjectId && pm.IsActive && pm.MemberRole == "PM");
                if (!isProjectPm)
                    return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該案件的 PM 可以重開案件" } });
            }

            if (original.Status != 50 && original.Status != 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Only closed or cancelled cases can be reopened" } });

            var userId = User.GetUserId();

            // 使用相同建案邏輯
            var project = await _db.Projects.AsNoTracking()
                .Include(p => p.ProjectCodeRef)
                .FirstOrDefaultAsync(p => p.ProjectId == original.ProjectId);
            var yearMonth = TimeHelper.TaipeiNow.ToString("yyyyMM");
            var codePrefix = project!.ProjectCodeRef?.Code ?? project.ProjectCode;
            var prefix = $"{codePrefix}-{yearMonth}-";
            var lastCase = await _db.Cases.AsNoTracking()
                .Where(c => c.CaseNumber.StartsWith(prefix))
                .OrderByDescending(c => c.CaseNumber)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (lastCase != null)
            {
                var parts = lastCase.CaseNumber.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[^1], out int lastSeq))
                    seq = lastSeq + 1;
            }

            var now = TimeHelper.Now;
            var newCase = new Case
            {
                CaseNumber = $"{prefix}{seq:D3}",
                ProjectId = original.ProjectId,
                CustomerId = original.CustomerId,
                CategoryId = original.CategoryId,
                ModuleId = original.ModuleId,
                ReporterName = original.ReporterName,
                ReporterPhone = original.ReporterPhone,
                ReporterEmail = original.ReporterEmail,
                CaseType = original.CaseType,
                Priority = original.Priority,
                Description = original.Description,
                Status = 10,
                CreatedBy = userId,
                RelatedCaseId = id,
                RelationType = "REOPEN",
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Cases.Add(newCase);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { slug = SlugHelper.Encode(newCase.CaseId) },
                new { success = true, data = new { id = newCase.CaseId, short_id = SlugHelper.Encode(newCase.CaseId), case_number = newCase.CaseNumber, related_case_id = id } });
        }

        #endregion

        #region 子資源寫入

        // POST /api/v1/cases/:id/logs  — 建立處理紀錄（含 §4.1 狀態轉換副作用）
        [HttpPost("{id:int}/logs")]
        public async Task<IActionResult> CreateLog(int id, [FromBody] CaseLogDto dto)
        {
            var caseEntity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (caseEntity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 終態不可再建 log
            if (caseEntity.Status == 50 || caseEntity.Status == 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot add log to closed or cancelled case", details = new { current_status = caseEntity.Status } } });

            // 工時範圍驗證 (DB 為 numeric(6,2) 上限 9999.99)
            if (dto.HoursSpent < 0 || dto.HoursSpent > 9999.99m)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "hours_spent must be between 0 and 9999.99" } });

            var handlerUserId = User.GetUserId();
            var handlerRole = User.GetRole();
            // 規則：SysAdmin、該專案 PM 一律可建立；SE 僅限「目前有效指派」才可建立
            var isPmOrAdmin = handlerRole == "SysAdmin"
                || await IsProjectMemberRoleAsync(handlerUserId, caseEntity.ProjectId, "PM");
            var isAssignedSe = !isPmOrAdmin && await _db.CaseAssignments.AsNoTracking()
                .AnyAsync(a => a.CaseId == id && a.SeUserId == handlerUserId && a.IsActive);
            if (!isPmOrAdmin && !isAssignedSe)
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 或目前指派的 SE 可以新增處理紀錄" } });

            var now = TimeHelper.Now;
            var isCompletion = dto.IsCompleted;

            // §4.1 狀態轉換矩陣（同 transaction）
            var prevStatus = caseEntity.Status;
            short newStatus = prevStatus;
            if (prevStatus == 10 || prevStatus == 20 || prevStatus == 35)
                newStatus = isCompletion ? (short)40 : (short)30;
            else if (prevStatus == 30)
                newStatus = isCompletion ? (short)40 : (short)30;
            // status=40: 維持 40（不回推）

            var statusAfterValue = isCompletion ? (short)40 : newStatus;

            var log = new CaseLog
            {
                CaseId = id,
                HandlerUserId = handlerUserId,
                LogDate = dto.LogDate ?? DateOnly.FromDateTime(TimeHelper.TaipeiNow),
                HandlingMethod = dto.HandlingMethod,
                HandlingResult = dto.HandlingResult,
                HoursSpent = dto.HoursSpent,
                Headcount = dto.Headcount > 0 ? dto.Headcount : (short)1,
                StatusAfter = statusAfterValue,
                RefCaseId = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CaseLogs.Add(log);
            caseEntity.Status = newStatus;
            caseEntity.UpdatedAt = now;

            // 首次到達 40 才發 WORK_COMPLETED 通知（多人派工僅首位觸發）
            if (prevStatus < 40 && newStatus == 40 && caseEntity.AssignedPmId.HasValue)
            {
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = caseEntity.AssignedPmId.Value,
                    CaseId = id,
                    NotificationType = "WORK_COMPLETED",
                    Title = $"案件完工 {caseEntity.CaseNumber}",
                    Message = "案件已完工，請確認是否結案",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = handlerUserId,
                CaseId = id,
                Action = isCompletion ? "WORK_COMPLETED" : "LOG_CREATED",
                EntityType = "case_log",
                EntityId = id,
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            return Created($"/api/v1/cases/{id}/logs/{log.LogId}", new { success = true, data = new { id = log.LogId, case_status = newStatus } });
        }

        // PATCH /api/v1/cases/:id/logs/:logId
        [HttpPatch("{id:int}/logs/{logId:int}")]
        public async Task<IActionResult> UpdateLog(int id, int logId, [FromBody] CaseLogDto dto)
        {
            var log = await _db.CaseLogs.FirstOrDefaultAsync(l => l.LogId == logId && l.CaseId == id);
            if (log == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });

            var caseForLog = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);

            if (dto.LogDate.HasValue) log.LogDate = dto.LogDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.HandlingMethod)) log.HandlingMethod = dto.HandlingMethod;
            if (dto.HandlingResult != null) log.HandlingResult = dto.HandlingResult;
            if (dto.HoursSpent > 0) log.HoursSpent = dto.HoursSpent;
            if (dto.Headcount > 0) log.Headcount = dto.Headcount;
            if (dto.RefCaseId.HasValue) log.RefCaseId = dto.RefCaseId == 0 ? null : dto.RefCaseId;
            var logNow = TimeHelper.Now;
            log.UpdatedAt = logNow;
            if (caseForLog != null) caseForLog.UpdatedAt = logNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = log.LogId } });
        }

        // POST /api/v1/cases/:id/estimations
        [HttpPost("{id:int}/estimations")]
        public async Task<IActionResult> CreateEstimation(int id, [FromBody] EstimationDto dto)
        {
            var caseEntity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (caseEntity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            var seqNo = await _db.CaseEstimations.Where(e => e.CaseId == id).CountAsync() + 1;

            var now = TimeHelper.Now;
            var est = new CaseEstimation
            {
                CaseId = id,
                EstimatorUserId = dto.EstimatorUserId,
                SeqNo = seqNo,
                RequestDate = dto.RequestDate ?? DateOnly.FromDateTime(TimeHelper.TaipeiNow),
                Summary = dto.Summary,
                EstimatedHours = dto.EstimatedHours,
                ReplyDate = dto.ReplyDate,
                EstimationStatus = dto.EstimationStatus > 0 ? dto.EstimationStatus : (short)10,
                Remarks = dto.Remarks,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CaseEstimations.Add(est);
            caseEntity.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return Created($"/api/v1/cases/{id}/estimations/{est.EstimationId}", new { success = true, data = new { id = est.EstimationId } });
        }

        // PATCH /api/v1/cases/:id/estimations/:eid
        [HttpPatch("{id:int}/estimations/{eid:int}")]
        public async Task<IActionResult> UpdateEstimation(int id, int eid, [FromBody] EstimationDto dto)
        {
            var est = await _db.CaseEstimations.FirstOrDefaultAsync(e => e.EstimationId == eid && e.CaseId == id);
            if (est == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Estimation not found" } });

            var oldStatus = est.EstimationStatus;

            if (dto.EstimatedHours > 0) est.EstimatedHours = dto.EstimatedHours;
            if (dto.ReplyDate.HasValue) est.ReplyDate = dto.ReplyDate;
            if (dto.EstimationStatus > 0) est.EstimationStatus = dto.EstimationStatus;
            if (dto.Remarks != null) est.Remarks = dto.Remarks;
            est.UpdatedAt = TimeHelper.Now;

            // 如果變更為已回覆 (30)，發通知給轉派 PM
            if (est.EstimationStatus == 30 && oldStatus != 30)
            {
                var caseEntity = await _db.Cases.AsNoTracking().FirstOrDefaultAsync(c => c.CaseId == id);
                if (caseEntity?.AssignedPmId != null)
                {
                    _db.Notifications.Add(new Notification
                    {
                        RecipientUserId = caseEntity.AssignedPmId.Value,
                        CaseId = id,
                        NotificationType = "ESTIMATION_DONE",
                        Title = $"工時評估完成 {caseEntity.CaseNumber}",
                        Message = "SE 已完成工時評估，請審閱",
                        IsRead = false,
                        CreatedAt = TimeHelper.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = est.EstimationId } });
        }

        // POST /api/v1/cases/:id/replies
        [HttpPost("{id:int}/replies")]
        public async Task<IActionResult> CreateReply(int id, [FromBody] ReplyDto dto)
        {
            var caseEntity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (caseEntity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (!await IsProjectPmAsync(User.GetUserId(), caseEntity.ProjectId))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以回覆客戶" } });

            var now = TimeHelper.Now;
            var reply = new CaseReply
            {
                CaseId = id,
                ReplierUserId = User.GetUserId(),
                ReplyDate = dto.ReplyDate ?? DateOnly.FromDateTime(TimeHelper.TaipeiNow),
                ReplyContent = dto.ReplyContent,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CaseReplies.Add(reply);
            caseEntity.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return Created($"/api/v1/cases/{id}/replies/{reply.ReplyId}", new { success = true, data = new { id = reply.ReplyId } });
        }

        // PATCH /api/v1/cases/:id/replies/:rid
        [HttpPatch("{id:int}/replies/{rid:int}")]
        public async Task<IActionResult> UpdateReply(int id, int rid, [FromBody] ReplyDto dto)
        {
            var reply = await _db.CaseReplies.FirstOrDefaultAsync(r => r.ReplyId == rid && r.CaseId == id);
            if (reply == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Reply not found" } });

            if (!string.IsNullOrWhiteSpace(dto.ReplyContent)) reply.ReplyContent = dto.ReplyContent;
            if (dto.ReplyDate.HasValue) reply.ReplyDate = dto.ReplyDate.Value;
            reply.UpdatedAt = TimeHelper.Now;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = reply.ReplyId } });
        }

        #endregion
    }

    #region DTOs

    public class CaseCreateDto
    {
        public int ProjectId { get; set; }
        public int CustomerId { get; set; }
        public int CategoryId { get; set; }
        public int? ModuleId { get; set; }
        public string ReporterName { get; set; } = "";
        public string? ReporterPhone { get; set; }
        public string? ReporterEmail { get; set; }
        public string? CaseType { get; set; }
        public string? Priority { get; set; }
        public string Description { get; set; } = "";
        public int? RelatedCaseId { get; set; }
    }

    public class CaseUpdateDto
    {
        public int? CategoryId { get; set; }
        public int? ModuleId { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterPhone { get; set; }
        public string? ReporterEmail { get; set; }
        public string? CaseType { get; set; }
        public string? Priority { get; set; }
        public string? Description { get; set; }
    }

    public class AssignDto
    {
        public List<int> SeUserIds { get; set; } = new();
        public int PrimarySeUserId { get; set; }
        public string? Instructions { get; set; }
        public DateOnly? ExpectedCompletionDate { get; set; }
    }

    public class ReturnDto
    {
        public string? Reason { get; set; }
    }

    public class CaseLogDto
    {
        public DateOnly? LogDate { get; set; }
        public string HandlingMethod { get; set; } = "";
        public string? HandlingResult { get; set; }
        public decimal HoursSpent { get; set; }
        public short Headcount { get; set; }
        /// <summary>true = 此筆 log 視為完工，觸發 → 40 狀態轉換（§4.1）</summary>
        public bool IsCompleted { get; set; } = false;
        /// <summary>引用的歷史案件 ID（可空）</summary>
        public int? RefCaseId { get; set; }
    }

    public class EstimationDto
    {
        public int EstimatorUserId { get; set; }
        public DateOnly? RequestDate { get; set; }
        public string Summary { get; set; } = "";
        public decimal EstimatedHours { get; set; }
        public DateOnly? ReplyDate { get; set; }
        public short EstimationStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class ReplyDto
    {
        public DateOnly? ReplyDate { get; set; }
        public string ReplyContent { get; set; } = "";
    }

    #endregion
}
