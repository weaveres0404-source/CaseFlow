$path='D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
$content=[System.IO.File]::ReadAllText($path,[System.Text.Encoding]::UTF8)

$pmOld='        private Task<bool> IsProjectPmAsync(int userId, int projectId)
        {
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == "PM");
        }'

$pmNew='        private Task<bool> IsProjectPmAsync(int userId, int projectId)
        {
            if (User.GetRole() == "SysAdmin") return Task.FromResult(true);
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == "PM");
        }'

$roleOld='        private Task<bool> IsProjectMemberRoleAsync(int userId, int projectId, string role)
        {
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == role);
        }'

$roleNew='        private Task<bool> IsProjectMemberRoleAsync(int userId, int projectId, string role)
        {
            if (User.GetRole() == "SysAdmin") return Task.FromResult(true);
            return _db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.UserId == userId
                    && pm.ProjectId == projectId
                    && pm.IsActive
                    && pm.MemberRole == role);
        }'

$c1=([regex]::Matches($content,[regex]::Escape($pmOld))).Count
$c2=([regex]::Matches($content,[regex]::Escape($roleOld))).Count
Write-Host "IsProjectPmAsync match=$c1  IsProjectMemberRoleAsync match=$c2"

if ($c1 -eq 1) { $content=$content.Replace($pmOld,$pmNew);   Write-Host "IsProjectPmAsync: patched" }
else { Write-Host "IsProjectPmAsync: SKIP" }

if ($c2 -eq 1) { $content=$content.Replace($roleOld,$roleNew); Write-Host "IsProjectMemberRoleAsync: patched" }
else { Write-Host "IsProjectMemberRoleAsync: SKIP" }

[System.IO.File]::WriteAllText($path,$content,[System.Text.Encoding]::UTF8)
$check = $content.Contains("SysAdmin") -and $content.Contains("Task.FromResult(true)")
Write-Host "Done. verify: has_sysadmin_bypass=$check"
