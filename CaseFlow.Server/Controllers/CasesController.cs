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

        public CasesController(CaseFlowDbContext db)
        {
            _db = db;
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
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var userId = User.GetUserId();
            var role = User.GetRole();

            // created_by_id is an alias for created_by
            if (created_by_id.HasValue && !created_by.HasValue) created_by = created_by_id;

            var query = _db.Cases.AsNoTracking()
                .Include(c => c.Project)
                .Include(c => c.Customer)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.AssignedPm)
                .Include(c => c.CaseAssignments.Where(a => a.IsActive))
                    .ThenInclude(a => a.SeUser)
                .AsQueryable();

            // 權限過濾
            if (role == "PM")
            {
                // 僅顯示「自己是轉派 PM」或「自己立案」的案件
                query = query.Where(c => c.AssignedPmId == userId || c.CreatedBy == userId);
            }
            else if (role == "SE")
            {
                var myCaseIds = await _db.CaseAssignments.AsNoTracking()
                    .Where(a => a.SeUserId == userId && a.IsActive)
                    .Select(a => a.CaseId).Distinct().ToListAsync();
                query = query.Where(c => myCaseIds.Contains(c.CaseId));
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

            // 快速分頁篩選
            if (assigned_to_me)
                query = query.Where(c => c.CaseAssignments.Any(a => a.SeUserId == userId && a.IsActive));
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
                    created_by = new { id = c.CreatedByNavigation.UserId, full_name = c.CreatedByNavigation.FullName },
                    assigned_pm = c.AssignedPm != null ? new { id = c.AssignedPm.UserId, full_name = c.AssignedPm.FullName } : null,
                    assigned_ses = c.CaseAssignments.Where(a => a.IsActive).Select(a => new { id = a.SeUser.UserId, full_name = a.SeUser.FullName }),
                    updated_at = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        // GET /api/v1/cases/:id — 案件詳情 (胖 Payload)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
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
                .Include(x => x.CaseEstimations).ThenInclude(e => e.EstimatorUser)
                .Include(x => x.CaseReplies).ThenInclude(r => r.ReplierUser)
                .FirstOrDefaultAsync(x => x.CaseId == id);

            if (c == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 權限檢查
            var viewerUserId = User.GetUserId();
            var viewerRole = User.GetRole();
            if (viewerRole == "PM")
            {
                if (!await HasProjectAccessAsync(c.ProjectId, viewerUserId))
                    return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "您無權存取此案件" } });
            }
            else if (viewerRole == "SE")
            {
                if (!await HasCaseAssignmentAsync(id, viewerUserId))
                    return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "您未被派工至此案件" } });
            }
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
                related_case = c.RelatedCase != null ? new { id = c.RelatedCase.CaseId, case_number = c.RelatedCase.CaseNumber } : null,
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
                    created_at = l.CreatedAt
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
            if (string.IsNullOrWhiteSpace(dto.ReporterName) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reporter_name and description are required" } });

            var userId = User.GetUserId();
            var creatorRole = User.GetRole();

            // SE 無權建立案件
            if (creatorRole == "SE")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "SE 無法建立新案件" } });

            // PM 只能對所屬專案立案
            if (creatorRole == "PM" && !await HasProjectAccessAsync(dto.ProjectId, userId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能對所屬專案立案" } });

            // 自動產生案件編號: 專案代碼-YYYYMM-流水號
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
            if (project == null)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid project_id" } });

            var yearMonth = TimeHelper.Now.ToString("yyyyMM");
            var prefix = $"{project.ProjectCode}-{yearMonth}-";
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
                CustomerId = dto.CustomerId,
                CategoryId = dto.CategoryId,
                ModuleId = dto.ModuleId,
                ReporterName = dto.ReporterName.Trim(),
                ReporterPhone = dto.ReporterPhone,
                ReporterEmail = dto.ReporterEmail,
                CaseType = dto.CaseType ?? "REPAIR",
                Priority = dto.Priority ?? "MEDIUM",
                Description = dto.Description,
                Status = 10, // 待處理
                CreatedBy = userId,
                CreatedAt = now,
                UpdatedAt = now,
                RelatedCaseId = dto.RelatedCaseId,
                RelationType = dto.RelatedCaseId.HasValue ? "REOPEN" : null
            };

            _db.Cases.Add(entity);

            // 確保立案人在 project_members 中有效紀錄
            await UpsertProjectMemberAsync(dto.ProjectId, userId, "PM", now);

            // 發送 CASE_CREATED 通知給專案所有 PM
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

            // Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "CASE_CREATED",
                EntityType = "case",
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            // 回填 audit log case_id
            var auditLog = await _db.AuditLogs.OrderByDescending(a => a.AuditId).FirstOrDefaultAsync(a => a.UserId == userId && a.Action == "CASE_CREATED");
            if (auditLog != null)
            {
                auditLog.CaseId = entity.CaseId;
                auditLog.EntityId = entity.CaseId;
                await _db.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = entity.CaseId },
                new { success = true, data = new { id = entity.CaseId, case_number = caseNumber } });
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

            // 角色檢查：僅 PM / SysAdmin 可執行派工
            var userId = User.GetUserId();
            var assignerRole = User.GetRole();
            if (assignerRole != "PM" && assignerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 PM / SysAdmin 可執行派工" } });

            // PM 權限範圍：僅能操作所屬專案的案件
            if (assignerRole == "PM" && !await HasProjectAccessAsync(entity.ProjectId, userId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能操作所屬專案的案件" } });

            var now = TimeHelper.Now;

            // 驗證所有被派工的 SE：若曾被明確移除（is_active=false），拒絕派工
            foreach (var seId in dto.SeUserIds)
            {
                var membership = await _db.ProjectMembers.FirstOrDefaultAsync(pm =>
                    pm.ProjectId == entity.ProjectId && pm.UserId == seId);
                if (membership != null && !membership.IsActive)
                {
                    var seName = await _db.Users
                        .Where(u => u.UserId == seId)
                        .Select(u => u.FullName)
                        .FirstOrDefaultAsync() ?? $"ID:{seId}";
                    return BadRequest(new
                    {
                        success = false,
                        error = new
                        {
                            code = "MEMBER_REMOVED",
                            message = $"{seName} 已從此專案移除，無法派工",
                            details = new { project_id = entity.ProjectId, se_user_id = seId }
                        }
                    });
                }
            }

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

            // 派工後一律重設為「已派工(20)」，確保新 SE 必須新增處理紀錄才能回報完工
            entity.Status = 20;
            entity.AssignedPmId = userId;
            entity.UpdatedAt = now;

            // 自動將首次派工的 SE 加入專案成員（僅當無既有記錄時才新增）
            foreach (var seId in dto.SeUserIds)
                await UpsertProjectMemberAsync(entity.ProjectId, seId, "SE", now);

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
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 角色檢查：SE / PM / SysAdmin 可回報完工
            var completerId = User.GetUserId();
            var completerRole = User.GetRole();
            if (completerRole != "SE" && completerRole != "PM" && completerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 SE / PM / SysAdmin 可回報完工" } });
            if (completerRole == "SE" && !await HasCaseAssignmentAsync(id, completerId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "您未被派工至此案件" } });

            // 必須先進入處理中 (30) 才能完工
            if (entity.Status != 30)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot complete from current status", details = new { current_status = entity.Status } } });

            // 必須有至少一筆處理紀錄（CaseLog.status_after = 30）才能完工
            var hasProcessingLog = await _db.CaseLogs.AnyAsync(l => l.CaseId == id && l.StatusAfter == 30);
            if (!hasProcessingLog)
                return Conflict(new { success = false, error = new { code = "NO_PROCESSING_LOG", message = "請先新增處理紀錄（狀態需進入處理中）才能回報完工" } });

            var now = TimeHelper.Now;
            entity.Status = 40;
            entity.UpdatedAt = now;

            // 自動補一筆 CaseLog（status_after = 40）
            _db.CaseLogs.Add(new CaseLog
            {
                CaseId = id,
                HandlerUserId = completerId,
                LogDate = DateOnly.FromDateTime(now),
                HandlingMethod = "回報完工",
                HandlingResult = null,
                HoursSpent = 0,
                Headcount = 1,
                StatusAfter = 40,
                CreatedAt = now,
                UpdatedAt = now
            });

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
            return Ok(new { success = true, data = new { id, status = 40 } });
        }

        // POST /api/v1/cases/:id/return — 退回
        [HttpPost("{id:int}/return")]
        public async Task<IActionResult> Return(int id, [FromBody] ReturnDto? dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 角色檢查：僅 PM / SysAdmin
            var returnerId = User.GetUserId();
            var returnerRole = User.GetRole();
            if (returnerRole != "PM" && returnerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 PM / SysAdmin 可執行退回" } });
            if (returnerRole == "PM" && !await HasProjectAccessAsync(entity.ProjectId, returnerId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能操作所屬專案的案件" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot return from current status", details = new { current_status = entity.Status } } });

            var now = TimeHelper.Now;
            entity.Status = 35;
            entity.UpdatedAt = now;

            _db.CaseLogs.Add(new CaseLog
            {
                CaseId = id,
                HandlerUserId = returnerId,
                LogDate = DateOnly.FromDateTime(now),
                HandlingMethod = $"案件退回{(string.IsNullOrWhiteSpace(dto?.Reason) ? "" : $"：{dto.Reason}")}",
                HandlingResult = null,
                HoursSpent = 0,
                Headcount = 1,
                StatusAfter = 35,
                CreatedAt = now,
                UpdatedAt = now
            });

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
            return Ok(new { success = true, data = new { id, status = 35 } });
        }

        // POST /api/v1/cases/:id/close — 結案
        [HttpPost("{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 角色檢查：僅 PM / SysAdmin
            var closerId = User.GetUserId();
            var closerRole = User.GetRole();
            if (closerRole != "PM" && closerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 PM / SysAdmin 可結案" } });
            if (closerRole == "PM" && !await HasProjectAccessAsync(entity.ProjectId, closerId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能操作所屬專案的案件" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot close from current status", details = new { current_status = entity.Status } } });

            var now = TimeHelper.Now;
            entity.Status = 50;
            entity.ClosedBy = User.GetUserId();
            entity.ClosedAt = now;
            entity.UpdatedAt = now;

            _db.CaseLogs.Add(new CaseLog
            {
                CaseId = id,
                HandlerUserId = closerId,
                LogDate = DateOnly.FromDateTime(now),
                HandlingMethod = "確認結案",
                HandlingResult = null,
                HoursSpent = 0,
                Headcount = 1,
                StatusAfter = 50,
                CreatedAt = now,
                UpdatedAt = now
            });

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "CASE_CLOSED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, status = 50 } });
        }

        // POST /api/v1/cases/:id/cancel — 取消
        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 角色檢查：僅 PM / SysAdmin
            var cancellerId = User.GetUserId();
            var cancellerRole = User.GetRole();
            if (cancellerRole != "PM" && cancellerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 PM / SysAdmin 可取消案件" } });
            if (cancellerRole == "PM" && !await HasProjectAccessAsync(entity.ProjectId, cancellerId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能操作所屬專案的案件" } });

            var allowed = new short[] { 10, 20, 30, 35, 40 };
            if (!allowed.Contains(entity.Status))
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot cancel from current status", details = new { current_status = entity.Status } } });

            var now = TimeHelper.Now;
            entity.Status = 60;
            entity.CancelledBy = cancellerId;
            entity.CancelledAt = now;
            entity.UpdatedAt = now;

            _db.CaseLogs.Add(new CaseLog
            {
                CaseId = id,
                HandlerUserId = cancellerId,
                LogDate = DateOnly.FromDateTime(now),
                HandlingMethod = $"取消案件{(string.IsNullOrWhiteSpace(dto?.Reason) ? "" : $"：{dto.Reason}")}",
                HandlingResult = null,
                HoursSpent = 0,
                Headcount = 1,
                StatusAfter = 60,
                CreatedAt = now,
                UpdatedAt = now
            });

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

            _db.AuditLogs.Add(new AuditLog { UserId = cancellerId, CaseId = id, Action = "CASE_CANCELLED", EntityType = "case", EntityId = id, CreatedAt = now });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, status = 60 } });
        }

        // POST /api/v1/cases/:id/reopen — 建新案件
        [HttpPost("{id:int}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var original = await _db.Cases.AsNoTracking().FirstOrDefaultAsync(x => x.CaseId == id);
            if (original == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // 角色檢查：僅 PM / SysAdmin
            var reopenerId = User.GetUserId();
            var reopenerRole = User.GetRole();
            if (reopenerRole != "PM" && reopenerRole != "SysAdmin")
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "只有 PM / SysAdmin 可重開案件" } });
            if (reopenerRole == "PM" && !await HasProjectAccessAsync(original.ProjectId, reopenerId))
                return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "PM 只能操作所屬專案的案件" } });

            if (original.Status != 50 && original.Status != 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Only closed or cancelled cases can be reopened" } });

            // 使用相同建案邏輯
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == original.ProjectId);
            var yearMonth = TimeHelper.Now.ToString("yyyyMM");
            var prefix = $"{project!.ProjectCode}-{yearMonth}-";
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
                CreatedBy = reopenerId,
                RelatedCaseId = id,
                RelationType = "REOPEN",
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Cases.Add(newCase);

            // 確保重開人在 project_members 中有效紀錄
            await UpsertProjectMemberAsync(original.ProjectId, reopenerId, "PM", now);

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newCase.CaseId },
                new { success = true, data = new { id = newCase.CaseId, case_number = newCase.CaseNumber, related_case_id = id } });
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

            var handlerUserId = User.GetUserId();
            var role = User.GetRole();

            // SE 權限檢查：必須在有效派工名單內，或為立案人 / Admin / SysAdmin
            if (role == "SE")
            {
                var isAssigned = await _db.CaseAssignments
                    .AnyAsync(a => a.CaseId == id && a.SeUserId == handlerUserId && a.IsActive);
                if (!isAssigned && caseEntity.CreatedBy != handlerUserId)
                    return StatusCode(403, new { success = false, error = new { code = "PERMISSION_DENIED", message = "You are not assigned to this case" } });
            }

            var now = TimeHelper.Now;
            var isCompletion = dto.IsCompleted;

            // §4.1 狀態轉換矩陣（同 transaction）
            var prevStatus = caseEntity.Status;
            short newStatus = prevStatus;
            // 10/20/35 → 30（處理中）；30 維持 30；需透過 /complete 才能到 40
            if (prevStatus == 10 || prevStatus == 20 || prevStatus == 35)
                newStatus = (short)30;
            else if (prevStatus == 30)
                newStatus = (short)30;
            // status=40: 維持 40（不回推）

            var statusAfterValue = newStatus;

            var log = new CaseLog
            {
                CaseId = id,
                HandlerUserId = handlerUserId,
                LogDate = dto.LogDate ?? DateOnly.FromDateTime(TimeHelper.Now),
                HandlingMethod = dto.HandlingMethod,
                HandlingResult = dto.HandlingResult,
                HoursSpent = dto.HoursSpent,
                Headcount = dto.Headcount > 0 ? dto.Headcount : (short)1,
                StatusAfter = statusAfterValue,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CaseLogs.Add(log);
            caseEntity.Status = newStatus;
            caseEntity.UpdatedAt = now;

            // 維護 total_hours 聚合欄位
            var currentTotalHours = await _db.CaseLogs
                .Where(l => l.CaseId == id)
                .SumAsync(l => (decimal?)l.HoursSpent) ?? 0m;
            caseEntity.TotalHours = currentTotalHours + log.HoursSpent;

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

            if (dto.LogDate.HasValue) log.LogDate = dto.LogDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.HandlingMethod)) log.HandlingMethod = dto.HandlingMethod;
            if (dto.HandlingResult != null) log.HandlingResult = dto.HandlingResult;
            if (dto.HoursSpent > 0) log.HoursSpent = dto.HoursSpent;
            if (dto.Headcount > 0) log.Headcount = dto.Headcount;
            log.UpdatedAt = TimeHelper.Now;

            await _db.SaveChangesAsync();

            // 重新計算 total_hours
            var recalc = await _db.CaseLogs.Where(l => l.CaseId == id).SumAsync(l => (decimal?)l.HoursSpent) ?? 0m;
            var caseForUpdate = await _db.Cases.FirstOrDefaultAsync(c => c.CaseId == id);
            if (caseForUpdate != null) { caseForUpdate.TotalHours = recalc; await _db.SaveChangesAsync(); }

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
                RequestDate = dto.RequestDate ?? DateOnly.FromDateTime(TimeHelper.Now),
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

            var now = TimeHelper.Now;
            var reply = new CaseReply
            {
                CaseId = id,
                ReplierUserId = User.GetUserId(),
                ReplyDate = dto.ReplyDate ?? DateOnly.FromDateTime(TimeHelper.Now),
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

        #region 私有輔助方法

        /// <summary>若使用者在 project_members 中不存在記錄，則新建；若已存在（含停用）則不修改。</summary>
        private async Task UpsertProjectMemberAsync(int projectId, int userId, string memberRole, DateTime now)
        {
            var existing = await _db.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (existing == null)
            {
                _db.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    MemberRole = memberRole,
                    JoinedAt = DateOnly.FromDateTime(now),
                    IsActive = true,
                    CreatedAt = now
                });
            }
            // 若記錄已存在（含 is_active=false），保持原狀不修改
        }

        /// <summary>PM 是否具有該專案的存取權（project_members is_active=TRUE）</summary>
        private async Task<bool> HasProjectAccessAsync(int projectId, int userId)
            => await _db.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == projectId && pm.UserId == userId && pm.IsActive);

        /// <summary>SE 是否有該案件的有效派工（case_assignments is_active=TRUE）</summary>
        private async Task<bool> HasCaseAssignmentAsync(int caseId, int userId)
            => await _db.CaseAssignments.AnyAsync(a =>
                a.CaseId == caseId && a.SeUserId == userId && a.IsActive);

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
