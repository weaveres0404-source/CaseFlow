"""
透過 Admin API 修正線上 problem_categories 中的錯誤分類名稱：
  PPC/工通知調查   → PP派工通知調查
  總部及應問題調查  → 總部應問題調查
"""
import requests, json

BASE = 'https://caseflow-test.sld-lwd.com'
r = requests.post(f'{BASE}/api/v1/auth/login', json={'username': 'admin', 'password': '@sld123456'})
tok = r.json()['data']['access_token']
h = {'Authorization': f'Bearer {tok}'}

# 1. 取得全部分類
r = requests.get(f'{BASE}/api/v1/problem-categories?page_size=200', headers=h)
cats = r.json().get('data', [])

FIXES = {
    'PPC/工通知調查':   'PP派工通知調查',
    '總部及應問題調查': '總部應問題調查',
}

fixed = 0
for cat in cats:
    name = cat.get('name') or cat.get('category_name', '')
    if name in FIXES:
        cid = cat.get('id') or cat.get('category_id')
        new_name = FIXES[name]
        payload = {
            'category_name': new_name,
            'description':   cat.get('description'),
            'sort_order':    cat.get('sort_order', 0),
            'case_type_filter': cat.get('case_type_filter'),
            'is_active':     cat.get('is_active', True),
            'project_ids':   [l['project_id'] for l in cat.get('project_links', [])] or cat.get('project_ids', []),
            'case_type_ids': [l['type_id'] for l in cat.get('case_type_links', [])] or cat.get('case_type_ids', []),
        }
        resp = requests.put(f'{BASE}/api/v1/problem-categories/{cid}', json=payload, headers=h)
        print(f'  [{resp.status_code}] id={cid}  "{name}" → "{new_name}"')
        fixed += 1

if fixed == 0:
    print('No matching categories found (may already be correct).')
    print('Available MAINTENANCE categories:')
    for cat in cats:
        if cat.get('case_type_filter') == 'MAINTENANCE':
            print(f'  id={cat.get("id")}  name={cat.get("name")}')
else:
    print(f'\nDone. Fixed {fixed} category name(s).')
