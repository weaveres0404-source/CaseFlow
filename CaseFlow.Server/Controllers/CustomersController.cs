using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        public CustomersController(CaseFlowDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, [FromQuery] int page_size = 20, [FromQuery] string? q = null)
        {
            if (page <= 0) page = 1;
            if (page_size <= 0 || page_size > 100) page_size = 20;

            var query = _db.Customers.AsNoTracking().Where(c => c.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.CustomerName, $"%{q}%") ||
                    EF.Functions.ILike(c.ContactPerson ?? "", $"%{q}%"));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.CustomerName)
                .Skip((page - 1) * page_size)
                .Take(page_size)
                .Select(c => new
                {
                    id = c.CustomerId,
                    customer_name = c.CustomerName,
                    contact_person = c.ContactPerson,
                    contact_phone = c.ContactPhone,
                    contact_email = c.ContactEmail,
                    address = c.Address,
                    remarks = c.Remarks,
                    is_active = c.IsActive,
                    created_at = c.CreatedAt,
                    updated_at = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = items, meta = new { page, page_size, total } });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Customer not found" } });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = c.CustomerId,
                    customer_name = c.CustomerName,
                    contact_person = c.ContactPerson,
                    contact_phone = c.ContactPhone,
                    contact_email = c.ContactEmail,
                    address = c.Address,
                    remarks = c.Remarks,
                    is_active = c.IsActive,
                    created_at = c.CreatedAt,
                    updated_at = c.UpdatedAt
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "customer_name is required" } });

            var now = DateTime.UtcNow;
            var entity = new Customer
            {
                CustomerName = dto.CustomerName.Trim(),
                ContactPerson = dto.ContactPerson,
                ContactPhone = dto.ContactPhone,
                ContactEmail = dto.ContactEmail,
                Address = dto.Address,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Customers.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.CustomerId }, new { success = true, data = new { id = entity.CustomerId } });
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
        {
            var entity = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Customer not found" } });

            if (!string.IsNullOrWhiteSpace(dto.CustomerName)) entity.CustomerName = dto.CustomerName.Trim();
            if (dto.ContactPerson != null) entity.ContactPerson = dto.ContactPerson;
            if (dto.ContactPhone != null) entity.ContactPhone = dto.ContactPhone;
            if (dto.ContactEmail != null) entity.ContactEmail = dto.ContactEmail;
            if (dto.Address != null) entity.Address = dto.Address;
            if (dto.Remarks != null) entity.Remarks = dto.Remarks;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = entity.CustomerId } });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (entity == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Customer not found" } });

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class CustomerDto
    {
        public string CustomerName { get; set; } = "";
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? Remarks { get; set; }
    }
}
