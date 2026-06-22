using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;
using CaseFlow.Server.Helpers;
using CaseFlow.Server.Services;

namespace CaseFlow.Server.Controllers
{
    [ApiController]
    [Route("api/v1/attachments")]
    [Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly CaseFlowDbContext _db;

        private readonly GcsStorageService _gcsStorageService;
        private const long MaxFileSize = 20 * 1024 * 1024; // 20MB

        public AttachmentsController(
         CaseFlowDbContext db,
         GcsStorageService gcsStorageService)
        {
            _db = db;
            _gcsStorageService = gcsStorageService;
        }

        // POST /api/v1/attachments
        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string entity_type, [FromForm] int entity_id)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "No file provided" } });

            if (file.Length > MaxFileSize)
                return StatusCode(413, new { success = false, error = new { code = "FILE_TOO_LARGE", message = "File size exceeds 20MB" } });

            var allowedTypes = new[] { "case", "case_log", "case_estimation", "case_reply" };
            if (!allowedTypes.Contains(entity_type))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "Invalid entity_type" } });

            var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var fileUrl = await _gcsStorageService.UploadAsync(
                file,
                storedName);

            var now = TimeHelper.Now;
            var attachment = new Attachment
            {
                FileName = file.FileName,
                StoredName = storedName,
                FilePath = fileUrl,
                FileSize = (int)file.Length,
                MimeType = file.ContentType ?? "application/octet-stream",
                EntityType = entity_type,
                EntityId = entity_id,
                UploadedBy = User.GetUserId(),
                UploadedAt = now
            };

            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync();

            return Created($"/api/v1/attachments/{attachment.AttachmentId}", new
            {
                success = true,
                data = new
                {
                    id = attachment.AttachmentId,
                    file_name = attachment.FileName,
                    file_size = attachment.FileSize,
                    mime_type = attachment.MimeType,
                    entity_type = attachment.EntityType,
                    entity_id = attachment.EntityId
                }
            });
        }

        // GET /api/v1/attachments/:id/download
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var att = await _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.AttachmentId == id);
            if (att == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Attachment not found" } });

            var stream = await _gcsStorageService.DownloadAsync(att.StoredName);

            return File(
                stream,
                att.MimeType,
                att.FileName);
        }

        // DELETE /api/v1/attachments/:id
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var att = await _db.Attachments.FirstOrDefaultAsync(a => a.AttachmentId == id);
            if (att == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Attachment not found" } });

            // 刪除檔案
            await _gcsStorageService.DeleteAsync(
                att.StoredName);

            _db.Attachments.Remove(att);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
