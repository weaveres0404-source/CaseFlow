$path='D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
$lines=Get-Content $path -Encoding UTF8

# Line 625 (index 624): if (!await IsProjectMemberRoleAsync(User.GetUserId(), entity.ProjectId, "SE"))
# Line 626 (index 625): return StatusCode(403 ... SE 可以回報完工
# Verify these are the correct lines
Write-Host "L625: $($lines[624])"
Write-Host "L626: $($lines[625])"

# Build new content: replace lines 625-626 with PM+SE check
$before = $lines[0..623]
$insert = @(
    '            var completeUserId = User.GetUserId();',
    '            var isSeForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "SE");',
    '            var isPmForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM");',
    '            if (!isSeForComplete && !isPmForComplete)',
    "                return StatusCode(403, new { success = false, error = new { code = `"FORBIDDEN`", message = `"" + [char]0x53EA + [char]0x6709 + [char]0x8A72 + [char]0x5C08 + [char]0x6848 + [char]0x7684 + " PM " + [char]0x6216 + " SE " + [char]0x53EF + [char]0x4EE5 + [char]0x56DE + [char]0x5831 + [char]0x5B8C + [char]0x5DE5 + `"`" } });"
)
$after = $lines[626..($lines.Length-1)]

$newContent = ($before + $insert + $after) -join "`n"
[System.IO.File]::WriteAllText($path, $newContent, [System.Text.Encoding]::UTF8)

# Verify
$check=Get-Content $path -Encoding UTF8
Write-Host "L625 after: $($check[624])"
Write-Host "L626 after: $($check[625])"
Write-Host "L627 after: $($check[626])"
Write-Host "Done"
