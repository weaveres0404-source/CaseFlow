using System;
using System.Collections.Generic;
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
    [Route("api/v1/case-types")]
    [Authorize]
    public class CaseTypesController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public CaseTypesController(CaseFlowDbContext db)
        {
            _db = db;
        }

        public class CaseTypeDto
        {
            public string Code { get; set; } = "";
            public string Label { get; set; } = "";
            public string? Description { get; set; }
            public string? Color { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; } = true;
            // 空 / null = 所有專案皆可用；有值 = 僅限指定專案
            public List<int>? ProjectIds { get; set; }
            // 空 / null = 不限問題分類；有值 = 僅當該案件類型被選時，這些分類才會出現
            public List<int>? CategoryIds { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int page_size = 50,
            [FromQuery] string? q = null,
            [FromQuery] int? project_id = null,
            [FromQuery] bool include_inactive = false)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 200) page_size = 50;

            var query = _db.CaseTypes.AsNoTracking()
                .Include(x => x.ProjectLinks)
                .Include(x => x.CategoryLinks)
                .AsQueryable();

            if (!include_inactive) query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLow = q.Trim();
                query = query.Where(x => EF.Functions.ILike(x.Code, $"%{qLow}%")
                                      || EF.Functions.ILike(x.Label, $"%{qLow}%"));
            }
            if (project_id.HasValue)
            {
                var pid = project_id.Value;
                query = query.Where(x => !x.ProjectLinks.Any() || x.ProjectLinks.Any(l => l.ProjectId == pid));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(x => new
                {
                    id = x.TypeId,
                    code = x.Code,
                    label = x.Label,
                    description = x.Description,
                    color = x.Color,
                    sort_order = x.SortOrder,
                    is_active = x.IsActive,
                    project_ids = x.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    category_ids = x.CategoryLinks.Select(l => l.CategoryId).ToList(),
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var t = await _db.CaseTypes.AsNoTracking()
                .Include(x => x.ProjectLinks)
                .Include(x => x.CategoryLinks)
                .FirstOrDefaultAsync(x => x.TypeId == id);
            if (t == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case type not found" } });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = t.TypeId,
                    code = t.Code,
                    label = t.Label,
                    description = t.Description,
                    color = t.Color,
                    sort_order = t.SortOrder,
                    is_active = t.IsActive,
                    project_ids = t.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    category_ids = t.CategoryLinks.Select(l => l.CategoryId).ToList(),
                    created_at = t.CreatedAt,
                    updated_at = t.UpdatedAt
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CaseTypeDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Label))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Code and Label are required" } });

            var now = TimeHelper.Now;
            var entity = new CaseType
            {
                Code = dto.Code.Trim().ToUpperInvariant(),
                Label = dto.Label.Trim(),
                Description = dto.Description,
                Color = dto.Color,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.CaseTypes.Add(entity);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not create case type", details = ex.Message } }); }

            if (dto.ProjectIds != null)
                foreach (var pid in dto.ProjectIds.Distinct())
                    _db.CaseTypeProjects.Add(new CaseTypeProject { TypeId = entity.TypeId, ProjectId = pid });
            if (dto.CategoryIds != null)
                foreach (var cid in dto.CategoryIds.Distinct())
                    _db.ProblemCategoryCaseTypes.Add(new ProblemCategoryCaseType { TypeId = entity.TypeId, CategoryId = cid });
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.TypeId }, new { success = true, data = new { id = entity.TypeId } });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CaseTypeDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Label))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Code and Label are required" } });

            var entity = await _db.CaseTypes
                .Include(x => x.ProjectLinks)
                .Include(x => x.CategoryLinks)
                .FirstOrDefaultAsync(x => x.TypeId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case type not found" } });

            entity.Code = dto.Code.Trim().ToUpperInvariant();
            entity.Label = dto.Label.Trim();
            entity.Description = dto.Description;
            entity.Color = dto.Color;
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = TimeHelper.Now;

            SyncLinks(entity.ProjectLinks, dto.ProjectIds, _db.CaseTypeProjects,
                      pid => new CaseTypeProject { TypeId = entity.TypeId, ProjectId = pid },
                      l => l.ProjectId);
            SyncLinks(entity.CategoryLinks, dto.CategoryIds, _db.ProblemCategoryCaseTypes,
                      cid => new ProblemCategoryCaseType { TypeId = entity.TypeId, CategoryId = cid },
                      l => l.CategoryId);

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not update case type", details = ex.Message } }); }

            return Ok(new { success = true, data = new { id = entity.TypeId } });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var entity = await _db.CaseTypes.FirstOrDefaultAsync(x => x.TypeId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Case type not found" } });
            if (!entity.IsActive)
                return BadRequest(new { success = false, error = new { code = "CONFLICT", message = "Already deleted" } });
            entity.IsActive = false;
            entity.UpdatedAt = TimeHelper.Now;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private static void SyncLinks<TLink>(
            ICollection<TLink> existing,
            List<int>? newIds,
            Microsoft.EntityFrameworkCore.DbSet<TLink> dbSet,
            Func<int, TLink> factory,
            Func<TLink, int> keySelector) where TLink : class
        {
            var newSet = (newIds ?? new List<int>()).Distinct().ToHashSet();
            foreach (var l in existing.ToList())
                if (!newSet.Contains(keySelector(l))) dbSet.Remove(l);
            var existingSet = existing.Select(keySelector).ToHashSet();
            foreach (var v in newSet)
                if (!existingSet.Contains(v)) dbSet.Add(factory(v));
        }
    }
}
