$path='D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
$lines=[System.IO.File]::ReadAllLines($path,[System.Text.Encoding]::UTF8)
$newLines=[System.Collections.Generic.List[string]]::new($lines)

# Find the SE-only check line in Complete method
$target1='            if (!await IsProjectMemberRoleAsync(User.GetUserId(), entity.ProjectId, "SE"))'
$target2='                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 SE 可以回報完工" } });'

$idx1 = -1
for ($i=0; $i -lt $newLines.Count; $i++) {
    if ($newLines[$i] -eq $target1) { $idx1=$i; break }
}
Write-Host "target1 at line index $idx1 (line $($idx1+1))"

if ($idx1 -ge 0 -and $newLines[$idx1+1] -eq $target2) {
    # Replace both lines with PM+SE check
    $newLines[$idx1]   = '            var completeUserId = User.GetUserId();'
    $newLines[$idx1+1] = '            var isSeForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "SE");'
    $newLines.Insert($idx1+2, '            var isPmForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM");')
    $newLines.Insert($idx1+3, '            if (!isSeForComplete && !isPmForComplete)')
    $newLines.Insert($idx1+4, '                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 或 SE 可以回報完工" } });')
    Write-Host "Fix1 (Complete PM): patched"
} else {
    Write-Host "Fix1: target not found or mismatch"
    if ($idx1 -ge 0) { Write-Host "line+1: $($newLines[$idx1+1])" }
}

[System.IO.File]::WriteAllLines($path, $newLines, [System.Text.Encoding]::UTF8)
Write-Host "Done. verify SE check gone=$(-not $([System.IO.File]::ReadAllText($path) -match '只有該專案的 SE 可以回報完工'))"
