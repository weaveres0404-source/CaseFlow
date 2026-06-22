with open(r'D:\CaseFlow\CaseFlow\caseflow.client\src\views\CaseDetail.vue', encoding='utf-8') as f:
    content = f.read()

old = "  if ([20, 30, 35].includes(s) && (isCurrentProjectSe.value || isCurrentProjectPm.value || auth.role.value === 'SysAdmin'))\n"
new = "  if ([20, 30, 35].includes(s) && (isCurrentProjectSe.value || isCurrentProjectPm.value || auth.role === 'SysAdmin'))\n"

count = content.count(old)
print(f'match={count}')
if count == 1:
    content = content.replace(old, new)
    with open(r'D:\CaseFlow\CaseFlow\caseflow.client\src\views\CaseDetail.vue', 'w', encoding='utf-8') as f:
        f.write(content)
    print('Fixed: auth.role.value -> auth.role')
else:
    print('Not found')
