using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/meta")]
    [Authorize]
    public class MetaController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;
        private readonly ILogger<MetaController> _logger;

        public MetaController(CaseFlowDbContext db, ILogger<MetaController> logger)
        {
            _db    = db;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/v1/meta/dropdowns — 一次回傳所有下拉選單
        /// </summary>
        [HttpGet("dropdowns")]
        public async Task<IActionResult> GetDropdowns()
        {
            try
            {
            var customers = await _db.Customers.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .Select(c => new { id = c.CustomerId, name = c.CustomerName })
                .ToListAsync();

            var projects = await _db.Projects.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectCode)
                .Select(p => new { id = p.ProjectId, code = p.ProjectCode, name = p.ProjectName, customer_id = p.CustomerId, allowed_case_types = p.AllowedCaseTypes })
                .ToListAsync();

            var categories = await _db.ProblemCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    id = c.CategoryId,
                    name = c.CategoryName,
                    case_type_filter = c.CaseTypeFilter,
                    project_ids = c.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    case_type_ids = c.CaseTypeLinks.Select(l => l.TypeId).ToList()
                })
                .ToListAsync();

            var projectCodes = await _db.ProjectCodes.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Code)
                .Select(p => new { id = p.CodeId, code = p.Code, label = p.Label })
                .ToListAsync();

            var caseTypes = await _db.CaseTypes.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Code)
                .Select(t => new
                {
                    id = t.TypeId,
                    code = t.Code,
                    label = t.Label,
                    color = t.Color,
                    project_ids = t.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    category_ids = t.CategoryLinks.Select(l => l.CategoryId).ToList()
                })
                .ToListAsync();

            var modules = await _db.SystemModules.AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModuleName)
                .Select(m => new { id = m.ModuleId, name = m.ModuleName, project_id = m.ProjectId })
                .ToListAsync();

            var users = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { id = u.UserId, username = u.Username, full_name = u.FullName, role = u.Role })
                .ToListAsync();

            var projectMembers = await _db.ProjectMembers.AsNoTracking()
                .Where(pm => pm.IsActive)
                .Select(pm => new { project_id = pm.ProjectId, user_id = pm.UserId, role = pm.MemberRole })
                .ToListAsync();

            var enums = new
            {
                // 保留 enum 區段以維持兼容；新前端應改讀頂層 case_types
                case_types = caseTypes.Select(t => new { value = t.code, label = t.label }).ToArray(),
                priorities = new[] {
                    new { value = "HIGH", label = "高" },
                    new { value = "MEDIUM", label = "中" },
                    new { value = "LOW", label = "低" }
                },
                statuses = new[] {
                    new { value = 10, label = "待處理" },
                    new { value = 20, label = "已派工" },
                    new { value = 30, label = "處理中" },
                    new { value = 35, label = "已退回" },
                    new { value = 40, label = "已完工" },
                    new { value = 50, label = "已結案" },
                    new { value = 60, label = "已取消" }
                },
                estimation_statuses = new[] {
                    new { value = 10, label = "待評估" },
                    new { value = 20, label = "評估中" },
                    new { value = 30, label = "已回覆" }
                }
            };

            return Ok(new
            {
                success = true,
                data = new { customers, projects, categories, modules, users, project_members = projectMembers, project_codes = projectCodes, case_types = caseTypes, enums }
            });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GetDropdowns failed | {ExType}: {Message}",
                    ex.GetType().FullName, ex.Message);
                Console.Error.WriteLine($"[META/DROPDOWNS] {ex}");
                throw; // ExceptionLoggingMiddleware + UseExceptionHandler will return HTTP 500
            }
        }
    }
}
