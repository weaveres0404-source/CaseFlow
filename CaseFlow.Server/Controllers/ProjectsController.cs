using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/projects")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public ProjectsController(CaseFlowDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/projects?page=1&page_size=20&q=keyword
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, [FromQuery] int page_size = 20, [FromQuery] string? q = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var query = _db.Projects.AsNoTracking()
                .Include(p => p.Customer)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.ProjectCode, $"%{q}%") ||
                    EF.Functions.ILike(p.ProjectName, $"%{q}%") ||
                    EF.Functions.ILike(p.Customer.CustomerName, $"%{q}%"));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.ProjectCode)
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(p => new
                {
                    id = p.ProjectId,
                    project_code = p.ProjectCode,
                    project_name = p.ProjectName,
                    customer = new { id = p.Customer.CustomerId, name = p.Customer.CustomerName },
                    description = p.Description,
                    start_date = p.StartDate,
                    end_date = p.EndDate,
                    is_active = p.IsActive,
                    created_at = p.CreatedAt,
                    updated_at = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        // GET /api/v1/projects/:id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _db.Projects.AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.ProjectMembers).ThenInclude(m => m.User)
                .Include(x => x.SystemModules)
                .FirstOrDefaultAsync(x => x.ProjectId == id);

            if (p == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project not found" } });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = p.ProjectId,
                    project_code = p.ProjectCode,
                    project_name = p.ProjectName,
                    customer = new { id = p.Customer.CustomerId, name = p.Customer.CustomerName },
                    description = p.Description,
                    start_date = p.StartDate,
                    end_date = p.EndDate,
                    is_active = p.IsActive,
                    members = p.ProjectMembers.Where(m => m.IsActive).Select(m => new
                    {
                        member_id = m.MemberId,
                        user_id = m.UserId,
                        full_name = m.User.FullName,
                        role = m.MemberRole,
                        joined_at = m.JoinedAt
                    }),
                    modules = p.SystemModules.Where(m => m.IsActive).Select(m => new
                    {
                        module_id = m.ModuleId,
                        module_name = m.ModuleName,
                        description = m.Description
                    }),
                    created_at = p.CreatedAt,
                    updated_at = p.UpdatedAt
                }
            });
        }

        // POST /api/v1/projects
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProjectCode) || string.IsNullOrWhiteSpace(dto.ProjectName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "project_code and project_name are required" } });

            var now = DateTime.UtcNow;
            var entity = new Project
            {
                ProjectCode = dto.ProjectCode.Trim(),
                ProjectName = dto.ProjectName.Trim(),
                CustomerId = dto.CustomerId,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Projects.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.ProjectId }, new { success = true, data = new { id = entity.ProjectId } });
        }

        // PATCH /api/v1/projects/:id
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectCreateDto dto)
        {
            var entity = await _db.Projects.FirstOrDefaultAsync(x => x.ProjectId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project not found" } });

            if (!string.IsNullOrWhiteSpace(dto.ProjectCode)) entity.ProjectCode = dto.ProjectCode.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ProjectName)) entity.ProjectName = dto.ProjectName.Trim();
            if (dto.CustomerId > 0) entity.CustomerId = dto.CustomerId;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.StartDate.HasValue) entity.StartDate = dto.StartDate;
            if (dto.EndDate.HasValue) entity.EndDate = dto.EndDate;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = entity.ProjectId } });
        }

        // DELETE /api/v1/projects/:id (soft)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Projects.FirstOrDefaultAsync(x => x.ProjectId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project not found" } });

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class ProjectCreateDto
    {
        public string ProjectCode { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public int CustomerId { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
