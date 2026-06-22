import requests, json

BASE = 'https://caseflow-test.sld-lwd.com/api/v1'
r = requests.post(f'{BASE}/auth/login', json={'username':'admin','password':'@sld123456'})
tok = r.json()['data']['access_token']
h = {'Authorization': f'Bearer {tok}'}

# Get all projects
r2 = requests.get(f'{BASE}/projects?page_size=100', headers=h)
projs = r2.json()
items = projs.get('data', {})
if isinstance(items, dict):
    items = items.get('items', items.get('data', []))

print(f'Total projects: {len(items)}')
for p in items:
    pid = p.get('project_id') or p.get('id')
    code = p.get('project_code') or p.get('code', '')
    name = p.get('project_name') or p.get('name', '')
    print(f'  pid={pid} code={code} name={name[:30]}')
    rm = requests.get(f'{BASE}/projects/{pid}', headers=h)
    if rm.status_code == 200:
        pdata = rm.json().get('data', {})
        members = pdata.get('members', [])
        for m in members:
            mid = m.get('member_id')
            uid = m.get('user_id')
            role = m.get('role')
            uname = m.get('username', '')
            print(f'    member_id={mid} user_id={uid} role={role} username={uname}')

# Also check meta/dropdowns as user_id=2 (need their username)
# Try to find user_id=2 by login token inspection
print('\n--- meta/dropdowns (as admin) ---')
rm = requests.get(f'{BASE}/meta/dropdowns', headers=h)
if rm.status_code == 200:
    data = rm.json().get('data', {})
    print('projects in meta:', [p.get('name') or p.get('project_name') for p in data.get('projects', [])])
