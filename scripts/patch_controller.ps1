$path='D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
$content=[System.IO.File]::ReadAllText($path,[System.Text.Encoding]::UTF8)

$returnOld='        public async Task<IActionResult> Return(int id, [FromBody] ReturnDto? dto)
        {
            var entity = await _db.Cases'

$returnNew='        public async Task<IActionResult> Return(int id, [FromBody] ReturnDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reason is required" } });

            var entity = await _db.Cases'

$cancelOld='        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)
        {
            var entity = await _db.Cases'

$cancelNew='        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reason is required" } });

            var entity = await _db.Cases'

$returnCount=([regex]::Matches($content,[regex]::Escape($returnOld))).Count
$cancelCount=([regex]::Matches($content,[regex]::Escape($cancelOld))).Count
Write-Host "Return match count: $returnCount"
Write-Host "Cancel match count: $cancelCount"

if ($returnCount -eq 1) {
    $content=$content.Replace($returnOld,$returnNew)
    Write-Host "Return: patched"
} else {
    Write-Host "Return: SKIP (no unique match)"
}

if ($cancelCount -eq 1) {
    $content=$content.Replace($cancelOld,$cancelNew)
    Write-Host "Cancel: patched"
} else {
    Write-Host "Cancel: SKIP (no unique match)"
}

[System.IO.File]::WriteAllText($path,$content,[System.Text.Encoding]::UTF8)
Write-Host "Done. Verifying..."
$check=[System.IO.File]::ReadAllText($path,[System.Text.Encoding]::UTF8)
Write-Host "has_reason_validation=$($check.Contains('reason is required'))"
