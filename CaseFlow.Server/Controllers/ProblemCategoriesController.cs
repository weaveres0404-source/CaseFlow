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
    [Route("api/v1/problem-categories")]
    [Authorize]
    public class ProblemCategoriesController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public ProblemCategoriesController(CaseFlowDbContext db)
        {
            _db = db;
        }

        public class CategoryDto
        {
            public string CategoryName { get; set; } = "";
            public string? Description { get; set; }
            public int SortOrder { get; set; }
            public string? CaseTypeFilter { get; set; }
            public bool IsActive { get; set; } = true;
            // 空陣列 / null = 共用所有專案；有值 = 多對多綁定指定專案集合
            public List<int>? ProjectIds { get; set; }
            // 空陣列 / null = 不限案件類型；有值 = 多對多綁定指定案件類型集合
            public List<int>? CaseTypeIds { get; set; }
        }

        // GET: api/v1/problem-categories
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int page_size = 20,
            [FromQuery] string? q = null,
            [FromQuery] int? project_id = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var query = _db.ProblemCategories.AsNoTracking()
                .Include(x => x.ProjectLinks)
                .Include(x => x.CaseTypeLinks)
                .Where(x => x.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLow = q.Trim();
                query = query.Where(x => EF.Functions.ILike(x.CategoryName, $"%{qLow}%")
                                     || EF.Functions.ILike(x.Description ?? string.Empty, $"%{qLow}%"));
            }

            if (project_id.HasValue)
            {
                var pid = project_id.Value;
                query = query.Where(x => !x.ProjectLinks.Any() || x.ProjectLinks.Any(l => l.ProjectId == pid));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CategoryName)
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(x => new
                {
                    id = x.CategoryId,
                    name = x.CategoryName,
                    description = x.Description,
                    sort_order = x.SortOrder,
                    case_type_filter = x.CaseTypeFilter,
                    project_ids = x.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    case_type_ids = x.CaseTypeLinks.Select(l => l.TypeId).ToList(),
                    is_active = x.IsActive,
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var cat = await _db.ProblemCategories.AsNoTracking()
                .Include(x => x.ProjectLinks)
                .Include(x => x.CaseTypeLinks)
                .FirstOrDefaultAsync(x => x.CategoryId == id && x.IsActive);
            if (cat == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = cat.CategoryId,
                    name = cat.CategoryName,
                    description = cat.Description,
                    sort_order = cat.SortOrder,
                    case_type_filter = cat.CaseTypeFilter,
                    project_ids = cat.ProjectLinks.Select(l => l.ProjectId).ToList(),
                    case_type_ids = cat.CaseTypeLinks.Select(l => l.TypeId).ToList(),
                    is_active = cat.IsActive,
                    created_at = cat.CreatedAt,
                    updated_at = cat.UpdatedAt
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CategoryName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "CategoryName is required" } });

            var now = TimeHelper.Now;
            var entity = new ProblemCategory
            {
                CategoryName = dto.CategoryName.Trim(),
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                CaseTypeFilter = string.IsNullOrWhiteSpace(dto.CaseTypeFilter) ? null : dto.CaseTypeFilter.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.ProblemCategories.Add(entity);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not create problem category", details = ex.Message } }); }

            if (dto.ProjectIds != null && dto.ProjectIds.Count > 0)
                foreach (var pid in dto.ProjectIds.Distinct())
                    _db.ProblemCategoryProjects.Add(new ProblemCategoryProject { CategoryId = entity.CategoryId, ProjectId = pid });
            if (dto.CaseTypeIds != null && dto.CaseTypeIds.Count > 0)
                foreach (var tid in dto.CaseTypeIds.Distinct())
                    _db.ProblemCategoryCaseTypes.Add(new ProblemCategoryCaseType { CategoryId = entity.CategoryId, TypeId = tid });
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.CategoryId }, new
            {
                success = true,
                data = new
                {
                    id = entity.CategoryId,
                    project_ids = dto.ProjectIds ?? new List<int>(),
                    case_type_ids = dto.CaseTypeIds ?? new List<int>()
                }
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CategoryDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CategoryName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "CategoryName is required" } });

            var entity = await _db.ProblemCategories
                .Include(x => x.ProjectLinks)
                .Include(x => x.CaseTypeLinks)
                .FirstOrDefaultAsync(x => x.CategoryId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            entity.CategoryName = dto.CategoryName.Trim();
            entity.Description = dto.Description;
            entity.SortOrder = dto.SortOrder;
            entity.CaseTypeFilter = string.IsNullOrWhiteSpace(dto.CaseTypeFilter) ? null : dto.CaseTypeFilter.Trim();
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = TimeHelper.Now;

            var newProjSet = (dto.ProjectIds ?? new List<int>()).Distinct().ToHashSet();
            foreach (var l in entity.ProjectLinks.ToList())
                if (!newProjSet.Contains(l.ProjectId)) _db.ProblemCategoryProjects.Remove(l);
            var existingProj = entity.ProjectLinks.Select(l => l.ProjectId).ToHashSet();
            foreach (var pid in newProjSet)
                if (!existingProj.Contains(pid))
                    _db.ProblemCategoryProjects.Add(new ProblemCategoryProject { CategoryId = entity.CategoryId, ProjectId = pid });

            var newTypeSet = (dto.CaseTypeIds ?? new List<int>()).Distinct().ToHashSet();
            foreach (var l in entity.CaseTypeLinks.ToList())
                if (!newTypeSet.Contains(l.TypeId)) _db.ProblemCategoryCaseTypes.Remove(l);
            var existingType = entity.CaseTypeLinks.Select(l => l.TypeId).ToHashSet();
            foreach (var tid in newTypeSet)
                if (!existingType.Contains(tid))
                    _db.ProblemCategoryCaseTypes.Add(new ProblemCategoryCaseType { CategoryId = entity.CategoryId, TypeId = tid });

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException ex) { return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not update problem category", details = ex.Message } }); }

            return Ok(new { success = true, data = new { id = entity.CategoryId, project_ids = newProjSet.ToList(), case_type_ids = newTypeSet.ToList() } });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var entity = await _db.ProblemCategories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            if (!entity.IsActive)
                return BadRequest(new { success = false, error = new { code = "CONFLICT", message = "Already deleted" } });

            entity.IsActive = false;
            entity.UpdatedAt = TimeHelper.Now;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}
