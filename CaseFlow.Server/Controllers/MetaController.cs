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

        public MetaController(CaseFlowDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/v1/meta/dropdowns — 一次回傳所有下拉選單
        /// </summary>
        [HttpGet("dropdowns")]
        public async Task<IActionResult> GetDropdowns()
        {
            var customers = await _db.Customers.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .Select(c => new { id = c.CustomerId, name = c.CustomerName })
                .ToListAsync();

            var projects = await _db.Projects.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectCode)
                .Select(p => new { id = p.ProjectId, code = p.ProjectCode, name = p.ProjectName, customer_id = p.CustomerId })
                .ToListAsync();

            var categories = await _db.ProblemCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { id = c.CategoryId, name = c.CategoryName, case_type_filter = c.CaseTypeFilter })
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
                case_types = new[] {
                    new { value = "REPAIR",      label = "障礙調查" },
                    new { value = "EVALUATION",  label = "工時評估" },
                    new { value = "MAINTENANCE", label = "日常維運" },
                    new { value = "UHD",         label = "UHD協助" }
                },
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
                data = new { customers, projects, categories, modules, users, project_members = projectMembers, enums }
            });
        }
    }
}
