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
            [FromQuery] string? sort = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var userId = User.GetUserId();
            var role = User.GetRole();

            var query = _db.Cases.AsNoTracking()
                .Include(c => c.Project)
                .Include(c => c.Customer)
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.AssignedPm)
                .Include(c => c.CaseAssignments.Where(a => a.IsActive && a.IsPrimary))
                    .ThenInclude(a => a.SeUser)
                .AsQueryable();

            // 權限過濾
            if (role == "PM")
            {
                var myProjectIds = await _db.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.IsActive && pm.MemberRole == "PM")
                    .Select(pm => pm.ProjectId).ToListAsync();
                query = query.Where(c => myProjectIds.Contains(c.ProjectId));
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
                    primary_se = c.CaseAssignments.Where(a => a.IsActive && a.IsPrimary).Select(a => new { id = a.SeUser.UserId, full_name = a.SeUser.FullName }).FirstOrDefault(),
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

            // SE 只能查看被分派給自己的案件
            if (User.GetRole() == "SE")
            {
                var viewerUserId = User.GetUserId();
                if (!c.CaseAssignments.Any(a => a.SeUserId == viewerUserId && a.IsActive))
                    return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "您未被分派至此案件" } });
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

        // POST /api/v1/cases — 建立案件（PM / SysAdmin / Admin 才能立案，SE 無權限）
        [HttpPost]
        [Authorize(Roles = "PM,SysAdmin,Admin")]
        public async Task<IActionResult> Create([FromBody] CaseCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ReporterName) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reporter_name and description are required" } });

            var userId = User.GetUserId();

            // 自動產生案件編號: 專案代碼-YYYYMM-流水號
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
            if (project == null)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid project_id" } });

            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
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

            var now = DateTime.UtcNow;

            // 解析客戶 id：若傳入新客戶名稱則建立
            int resolvedCustomerId;
            if (dto.CustomerId.HasValue && dto.CustomerId.Value > 0)
            {
                resolvedCustomerId = dto.CustomerId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                // 查找同名客戶，沒有則建立
                var existing = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerName == dto.CustomerName.Trim());
                if (existing != null)
                {
                    resolvedCustomerId = existing.CustomerId;
                }
                else
                {
                    var newCustomer = new Customer
                    {
                        CustomerName = dto.CustomerName.Trim(),
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _db.Customers.Add(newCustomer);
                    await _db.SaveChangesAsync();
                    resolvedCustomerId = newCustomer.CustomerId;
                }
            }
            else
            {
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "客戶為必選欄位，請選擇現有客戶或輸入新客戶名稱" } });
            }

            var entity = new Case
            {
                CaseNumber = caseNumber,
                ProjectId = dto.ProjectId,
                CustomerId = resolvedCustomerId,
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
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = entity.CaseId } });
        }

        #region RPC 狀態流轉

        // POST|PATCH /api/v1/cases/:id/assign — 派工（PM / SysAdmin / Admin 才能派工）
        [HttpPost("{id:int}/assign")]
        [HttpPatch("{id:int}/assign")]
        [Authorize(Roles = "PM,SysAdmin,Admin")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignDto dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 10 && entity.Status != 20 && entity.Status != 35)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot assign from current status", details = new { current_status = entity.Status } } });

            var userId = User.GetUserId();
            var now = DateTime.UtcNow;

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

            entity.Status = 20;
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

        // POST /api/v1/cases/:id/start
        [HttpPost("{id:int}/start")]
        public async Task<IActionResult> Start(int id)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 20 && entity.Status != 35)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot start from current status", details = new { current_status = entity.Status } } });

            entity.Status = 30;
            entity.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog { UserId = User.GetUserId(), CaseId = id, Action = "CASE_STARTED", EntityType = "case", EntityId = id, CreatedAt = DateTime.UtcNow });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id, status = 30 } });
        }

        // POST /api/v1/cases/:id/complete
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 30)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot complete from current status", details = new { current_status = entity.Status } } });

            var now = DateTime.UtcNow;
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
            return Ok(new { success = true, data = new { id, status = 40 } });
        }

        // POST /api/v1/cases/:id/return — 退回（PM / SysAdmin / Admin 才能退回）
        [HttpPost("{id:int}/return")]
        [Authorize(Roles = "PM,SysAdmin,Admin")]
        public async Task<IActionResult> Return(int id, [FromBody] ReturnDto? dto)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot return from current status", details = new { current_status = entity.Status } } });

            var now = DateTime.UtcNow;
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
            return Ok(new { success = true, data = new { id, status = 35 } });
        }

        // POST /api/v1/cases/:id/close — 結案（PM / SysAdmin / Admin 才能結案）
        [HttpPost("{id:int}/close")]
        [Authorize(Roles = "PM,SysAdmin,Admin")]
        public async Task<IActionResult> Close(int id)
        {
            var entity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (entity.Status != 40)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot close from current status", details = new { current_status = entity.Status } } });

            var now = DateTime.UtcNow;
            entity.Status = 50;
            entity.ClosedBy = User.GetUserId();
            entity.ClosedAt = now;
            entity.UpdatedAt = now;

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

            var allowed = new short[] { 10, 20, 30, 35, 40 };
            if (!allowed.Contains(entity.Status))
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Cannot cancel from current status", details = new { current_status = entity.Status } } });

            var now = DateTime.UtcNow;
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
            return Ok(new { success = true, data = new { id, status = 60 } });
        }

        // POST /api/v1/cases/:id/reopen — 建新案件
        [HttpPost("{id:int}/reopen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var original = await _db.Cases.AsNoTracking().FirstOrDefaultAsync(x => x.CaseId == id);
            if (original == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            if (original.Status != 50 && original.Status != 60)
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Only closed or cancelled cases can be reopened" } });

            var userId = User.GetUserId();

            // 使用相同建案邏輯
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == original.ProjectId);
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
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

            var now = DateTime.UtcNow;
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

            return CreatedAtAction(nameof(GetById), new { id = newCase.CaseId },
                new { success = true, data = new { id = newCase.CaseId, case_number = newCase.CaseNumber, related_case_id = id } });
        }

        #endregion

        #region 子資源寫入

        // POST /api/v1/cases/:id/logs
        [HttpPost("{id:int}/logs")]
        public async Task<IActionResult> CreateLog(int id, [FromBody] CaseLogDto dto)
        {
            var caseEntity = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);
            if (caseEntity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case not found" } });

            // SE 只能在被分派給自己的案件上新增處理紀錄
            var handlerRole = User.GetRole();
            var handlerUserId = User.GetUserId();
            if (handlerRole == "SE")
            {
                var isAssigned = await _db.CaseAssignments
                    .AnyAsync(a => a.CaseId == id && a.SeUserId == handlerUserId && a.IsActive);
                if (!isAssigned)
                    return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "您未被分派至此案件" } });
            }
            var now = DateTime.UtcNow;
            // 若呼叫端未指定 status_after，維持現有案件狀態
            var statusAfter = dto.StatusAfter > 0 ? dto.StatusAfter : caseEntity.Status;
            var log = new CaseLog
            {
                CaseId = id,
                HandlerUserId = handlerUserId,
                LogDate = dto.LogDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                HandlingMethod = dto.HandlingMethod,
                HandlingResult = dto.HandlingResult,
                HoursSpent = dto.HoursSpent,
                Headcount = dto.Headcount > 0 ? dto.Headcount : (short)1,
                StatusAfter = statusAfter,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CaseLogs.Add(log);

            // ── 工時動態累計（TotalHours 為 NotMapped，不寫 DB）──────────
            var existingHours = await _db.CaseLogs.Where(l => l.CaseId == id).SumAsync(l => l.HoursSpent);
            var currentTotalHours = existingHours + dto.HoursSpent;

            // ── 依 status_after 自動更新案件狀態（狀態流轉）──────────────
            if (statusAfter != caseEntity.Status)
            {
                caseEntity.Status = statusAfter;

                // SE 回報完工 (40 已完工) → 通知轉派 PM
                if (statusAfter == 40 && caseEntity.AssignedPmId.HasValue)
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
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = handlerUserId,
                CaseId = id,
                Action = "LOG_ADDED",
                EntityType = "case_log",
                EntityId = id,
                CreatedAt = now
            });

            caseEntity.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return Created($"/api/v1/cases/{id}/logs/{log.LogId}", new { success = true, data = new { id = log.LogId, status = caseEntity.Status, total_hours = currentTotalHours } });
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
            if (dto.StatusAfter > 0) log.StatusAfter = dto.StatusAfter;
            log.UpdatedAt = DateTime.UtcNow;

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

            var now = DateTime.UtcNow;
            var est = new CaseEstimation
            {
                CaseId = id,
                EstimatorUserId = dto.EstimatorUserId,
                SeqNo = seqNo,
                RequestDate = dto.RequestDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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
            est.UpdatedAt = DateTime.UtcNow;

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
                        CreatedAt = DateTime.UtcNow
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

            var now = DateTime.UtcNow;
            var reply = new CaseReply
            {
                CaseId = id,
                ReplierUserId = User.GetUserId(),
                ReplyDate = dto.ReplyDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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
            reply.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = reply.ReplyId } });
        }

        #endregion
    }

    #region DTOs

    public class CaseCreateDto
    {
        public int ProjectId { get; set; }
        public int? CustomerId { get; set; }   // nullable：儲存現有客戶 id
        public string? CustomerName { get; set; } // 新客戶名稱
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
        public short StatusAfter { get; set; }
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
