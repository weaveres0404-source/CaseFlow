with open(r'D:\CaseFlow\CaseFlow\caseflow.client\src\views\CaseDetail.vue', encoding='utf-8') as f:
    content = f.read()

old = (
    "  // 回報完工: [30] only，該專案 SE — 必須先進入處理中才可完工\n"
    "  if (s === 30 && isCurrentProjectSe.value)\n"
    "    actions.push({ label: '回報完工', icon: I.check, handler: () => doAction('complete', '確認此案件已完工？'), class: primaryActionClass })\n"
)

new = (
    "  // 回報完工: [20,30,35]，該專案 SE 或 PM 或 SysAdmin\n"
    "  if ([20, 30, 35].includes(s) && (isCurrentProjectSe.value || isCurrentProjectPm.value || auth.role.value === 'SysAdmin'))\n"
    "    actions.push({ label: '回報完工', icon: I.check, handler: () => doAction('complete', '確認此案件已完工？'), class: primaryActionClass })\n"
)

count = content.count(old)
print(f'match={count}')

if count == 1:
    content = content.replace(old, new)
    with open(r'D:\CaseFlow\CaseFlow\caseflow.client\src\views\CaseDetail.vue', 'w', encoding='utf-8') as f:
        f.write(content)
    print('Frontend complete button: patched')
    with open(r'D:\CaseFlow\CaseFlow\caseflow.client\src\views\CaseDetail.vue', encoding='utf-8') as f:
        check = f.read()
    print(f'has_pm_condition={"isCurrentProjectPm.value" in check and "[20, 30, 35]" in check}')
else:
    # show actual lines
    lines = content.splitlines()
    for i, l in enumerate(lines[1035:1042], start=1036):
        print(f'{i}: {repr(l)}')
