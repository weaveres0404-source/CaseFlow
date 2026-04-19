using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: api/v1/problem-categories?page=1&page_size=20&q=keyword
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int page_size = 20, [FromQuery] string? q = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var query = _db.ProblemCategories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLow = q.Trim();
                query = query.Where(x => EF.Functions.ILike(x.CategoryName, $"%{qLow}%") || EF.Functions.ILike(x.Description ?? string.Empty, $"%{qLow}%"));
            }

            // Only active by default
            query = query.Where(x => x.IsActive);

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
                    is_active = x.IsActive,
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        // GET api/v1/problem-categories/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var cat = await _db.ProblemCategories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryId == id && x.IsActive);
            if (cat == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            var result = new
            {
                id = cat.CategoryId,
                name = cat.CategoryName,
                description = cat.Description,
                sort_order = cat.SortOrder,
                is_active = cat.IsActive,
                created_at = cat.CreatedAt,
                updated_at = cat.UpdatedAt
            };

            return Ok(new { success = true, data = result });
        }

        // POST api/v1/problem-categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProblemCategory dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CategoryName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "CategoryName is required" } });

            var now = DateTime.UtcNow;
            var entity = new ProblemCategory
            {
                CategoryName = dto.CategoryName.Trim(),
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.ProblemCategories.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not create problem category", details = ex.Message } });
            }

            var result = new
            {
                id = entity.CategoryId,
                name = entity.CategoryName,
                description = entity.Description,
                sort_order = entity.SortOrder,
                is_active = entity.IsActive,
                created_at = entity.CreatedAt,
                updated_at = entity.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.CategoryId }, new { success = true, data = result });
        }

        // PUT api/v1/problem-categories/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProblemCategory dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CategoryName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "CategoryName is required" } });

            var entity = await _db.ProblemCategories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            entity.CategoryName = dto.CategoryName.Trim();
            entity.Description = dto.Description;
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { success = false, error = new { code = "CONFLICT", message = "Could not update problem category", details = ex.Message } });
            }

            var result = new
            {
                id = entity.CategoryId,
                name = entity.CategoryName,
                description = entity.Description,
                sort_order = entity.SortOrder,
                is_active = entity.IsActive,
                created_at = entity.CreatedAt,
                updated_at = entity.UpdatedAt
            };

            return Ok(new { success = true, data = result });
        }

        // DELETE api/v1/problem-categories/5  (soft delete)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var entity = await _db.ProblemCategories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Problem category not found" } });

            if (!entity.IsActive)
                return BadRequest(new { success = false, error = new { code = "CONFLICT", message = "Already deleted" } });

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
