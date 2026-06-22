import re

path = r'D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'

with open(path, encoding='utf-8') as f:
    content = f.read()

old = (
    '            if (!await IsProjectMemberRoleAsync(User.GetUserId(), entity.ProjectId, "SE"))\n'
    '                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 SE 可以回報完工" } });\n'
)

new = (
    '            var completeUserId = User.GetUserId();\n'
    '            var isSeForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "SE");\n'
    '            var isPmForComplete = await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM");\n'
    '            if (!isSeForComplete && !isPmForComplete)\n'
    '                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 或 SE 可以回報完工" } });\n'
)

count = content.count(old)
print(f'Fix1 match={count}')

if count == 1:
    content = content.replace(old, new)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print('Fix1 (Complete PM): patched')
    # verify
    with open(path, encoding='utf-8') as f:
        check = f.read()
    print(f'has_pm_check={("isPmForComplete" in check)}')
    print(f'has_old_se_only={"只有該專案的 SE 可以回報完工" in check}')
else:
    # Try CRLF
    old_crlf = old.replace('\n', '\r\n')
    count2 = content.count(old_crlf)
    print(f'Fix1 CRLF match={count2}')
    if count2 == 1:
        content = content.replace(old_crlf, new)
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print('Fix1 (Complete PM): patched (CRLF)')
    else:
        # Show actual lines 624-626 for debugging
        lines = content.splitlines()
        for i in range(623, 628):
            print(f'  L{i+1}: {repr(lines[i])}')
