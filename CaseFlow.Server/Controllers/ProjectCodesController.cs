using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Helpers;
using CaseFlow.Server.Models;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/project-codes")]
    [Authorize]
    public class ProjectCodesController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public ProjectCodesController(CaseFlowDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/project-codes?q=&page=&page_size=
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int page_size = 50, [FromQuery] string? q = null, [FromQuery] bool include_inactive = false)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 200) page_size = 50;

            var query = _db.ProjectCodes.AsNoTracking().AsQueryable();
            if (!include_inactive) query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => EF.Functions.ILike(x.Code, $"%{q}%") || EF.Functions.ILike(x.Label, $"%{q}%"));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
                .Skip((page - 1) * page_size).Take(page_size)
                .Select(x => new
                {
                    id = x.CodeId,
                    code = x.Code,
                    label = x.Label,
                    description = x.Description,
                    sort_order = x.SortOrder,
                    is_active = x.IsActive,
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var x = await _db.ProjectCodes.AsNoTracking().FirstOrDefaultAsync(c => c.CodeId == id);
            if (x == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project code not found" } });
            return Ok(new
            {
                success = true,
                data = new
                {
                    id = x.CodeId,
                    code = x.Code,
                    label = x.Label,
                    description = x.Description,
                    sort_order = x.SortOrder,
                    is_active = x.IsActive,
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                }
            });
        }

        public class ProjectCodeDto
        {
            public string Code { get; set; } = "";
            public string Label { get; set; } = "";
            public string? Description { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Label))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "code and label are required" } });

            var now = TimeHelper.TaipeiNow;
            var entity = new ProjectCode
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Label = dto.Label.Trim(),
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.ProjectCodes.Add(entity);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not create project code", details = ex.Message } }); }

            return CreatedAtAction(nameof(GetById), new { id = entity.CodeId }, new { success = true, data = new { id = entity.CodeId } });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectCodeDto dto)
        {
            var entity = await _db.ProjectCodes.FirstOrDefaultAsync(x => x.CodeId == id);
            if (entity == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project code not found" } });

            if (!string.IsNullOrWhiteSpace(dto.Code)) entity.Code = dto.Code.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(dto.Label)) entity.Label = dto.Label.Trim();
            entity.Description = dto.Description;
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = TimeHelper.TaipeiNow;

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not update project code", details = ex.Message } }); }

            return Ok(new { success = true, data = new { id = entity.CodeId } });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.ProjectCodes.FirstOrDefaultAsync(x => x.CodeId == id);
            if (entity == null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Project code not found" } });
            entity.IsActive = false;
            entity.UpdatedAt = TimeHelper.TaipeiNow;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}
