path = r'D:\CaseFlow\CaseFlow\CaseFlow.Server\Controllers\CasesController.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = (
    '        // POST /api/v1/cases/:id/cancel \u2014 \u53d6\u6d88\n'
    '        [HttpPost("{id:int}/cancel")]\n'
    '        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)\n'
    '        {\n'
    '            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))\n'
    '                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "reason is required" } });\n'
)

new = (
    '        // POST /api/v1/cases/:id/cancel \u2014 \u53d6\u6d88\n'
    '        [HttpPost("{id:int}/cancel")]\n'
    '        public async Task<IActionResult> Cancel(int id, [FromBody] ReturnDto? dto)\n'
    '        {\n'
)

if old in content:
    content = content.replace(old, new, 1)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print('OK: removed mandatory reason validation from Cancel')
else:
    print('ERROR: pattern not found')
    # debug
    idx = content.find('cases/:id/cancel')
    if idx >= 0:
        print('Found cancel section at index', idx)
        print(repr(content[idx:idx+400]))
