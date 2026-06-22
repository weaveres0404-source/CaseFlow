import re

path = r'D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern to match the current PM+SE check (handles both CRLF and LF)
pattern = re.compile(
    r'            var completeUserId = User\.GetUserId\(\);\s+'
    r'            var isSeForComplete = await IsProjectMemberRoleAsync\(completeUserId, entity\.ProjectId, "SE"\);\s+'
    r'            var isPmForComplete = await IsProjectMemberRoleAsync\(completeUserId, entity\.ProjectId, "PM"\);\s+'
    r'            if \(!isSeForComplete && !isPmForComplete\)\s+'
    r'                return StatusCode\(403, new \{ success = false, error = new \{ code = "FORBIDDEN", message = "只有該專案的 PM 或 SE 可以回報完工" \} \}\);'
)

new_code = '''            var completeUserId = User.GetUserId();
            if (!await IsProjectMemberRoleAsync(completeUserId, entity.ProjectId, "PM"))
                return StatusCode(403, new { success = false, error = new { code = "FORBIDDEN", message = "只有該專案的 PM 可以回報完工" } });'''

m = pattern.search(content)
if m:
    content = content[:m.start()] + new_code + content[m.end():]
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS: Complete endpoint patched to PM-only')
else:
    print('NOT FOUND, showing lines around complete endpoint:')
    lines = content.split('\n')
    for i, l in enumerate(lines[620:635], 621):
        print(f'{i}: {repr(l[:80])}')
