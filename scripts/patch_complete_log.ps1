$path='D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
$content=[System.IO.File]::ReadAllText($path,[System.Text.Encoding]::UTF8)

# Fix 1: Complete endpoint — allow PM or SE (SysAdmin already handled by IsProjectMemberRoleAsync bypass)
$old1='            if (!await IsProjectMemberRoleAsync(User.GetUserId(), entity.ProjectId, "SE"))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 SE 可以回報完工" } });'

$new1='            var completeUserId = User.GetUserId();
            var isSeForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "SE");
            var isPmForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM");
            if (!isSeForComplete && !isPmForComplete)
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 或 SE 可以回報完工" } });'

# Fix 2: UpdateLog — also update caseEntity.UpdatedAt
$old2='            var log = await _db.CaseLogs.FirstOrDefaultAsync(l => l.LogId == logId && l.CaseId == id);
            if (log == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });

            if (dto.LogDate.HasValue) log.LogDate = dto.LogDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.HandlingMethod)) log.HandlingMethod = dto.HandlingMethod;
            if (dto.HandlingResult != null) log.HandlingResult = dto.HandlingResult;
            if (dto.HoursSpent > 0) log.HoursSpent = dto.HoursSpent;
            if (dto.Headcount > 0) log.Headcount = dto.Headcount;
            if (dto.RefCaseId.HasValue) log.RefCaseId = dto.RefCaseId == 0 ? null : dto.RefCaseId;
            log.UpdatedAt = TimeHelper.Now;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = log.LogId } });'

$new2='            var log = await _db.CaseLogs.FirstOrDefaultAsync(l => l.LogId == logId && l.CaseId == id);
            if (log == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Log not found" } });

            var caseForLog = await _db.Cases.FirstOrDefaultAsync(x => x.CaseId == id);

            if (dto.LogDate.HasValue) log.LogDate = dto.LogDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.HandlingMethod)) log.HandlingMethod = dto.HandlingMethod;
            if (dto.HandlingResult != null) log.HandlingResult = dto.HandlingResult;
            if (dto.HoursSpent > 0) log.HoursSpent = dto.HoursSpent;
            if (dto.Headcount > 0) log.Headcount = dto.Headcount;
            if (dto.RefCaseId.HasValue) log.RefCaseId = dto.RefCaseId == 0 ? null : dto.RefCaseId;
            var logNow = TimeHelper.Now;
            log.UpdatedAt = logNow;
            if (caseForLog != null) caseForLog.UpdatedAt = logNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = new { id = log.LogId } });'

$c1=([regex]::Matches($content,[regex]::Escape($old1))).Count
$c2=([regex]::Matches($content,[regex]::Escape($old2))).Count
Write-Host "Fix1 match=$c1  Fix2 match=$c2"

if ($c1 -eq 1) { $content=$content.Replace($old1,$new1); Write-Host "Fix1 (Complete PM): patched" }
else { Write-Host "Fix1: SKIP (count=$c1)" }

if ($c2 -eq 1) { $content=$content.Replace($old2,$new2); Write-Host "Fix2 (UpdateLog case.UpdatedAt): patched" }
else { Write-Host "Fix2: SKIP (count=$c2)" }

[System.IO.File]::WriteAllText($path,$content,[System.Text.Encoding]::UTF8)
Write-Host "Done."
