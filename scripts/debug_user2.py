"""診斷 user_id=2 無法建案的原因"""
import requests, json

BASE = 'https://caseflow-test.sld-lwd.com/api/v1'

# 1. admin 登入
r = requests.post(f'{BASE}/auth/login', json={'username':'admin','password':'@sld123456'})
admin_tok = r.json()['data']['access_token']
ha = {'Authorization': f'Bearer {admin_tok}'}
print('[admin login] OK')

# 2. 取得所有 users
r = requests.get(f'{BASE}/admin/users?page_size=100', headers=ha)
if r.status_code != 200:
    print('[admin/users] status=', r.status_code, r.text[:300])
else:
    users = r.json().get('data', {})
    items = users.get('items', users) if isinstance(users, dict) else users
    print(f'[admin/users] total={len(items)}')
    for u in items:
        uid = u.get('user_id') or u.get('id')
        print(f"  id={uid} username={u.get('username')} role={u.get('role')} name={u.get('full_name') or u.get('display_name','')}")

# 3. 取得專案成員列表（用 admin）
r = requests.get(f'{BASE}/projects?page_size=100', headers=ha)
if r.status_code == 200:
    projs = r.json().get('data', {})
    items_p = projs.get('items', projs) if isinstance(projs, dict) else projs
    print(f'\n[projects] total={len(items_p)}')
    for p in items_p:
        pid = p.get('project_id') or p.get('id')
        print(f"  id={pid} code={p.get('project_code') or p.get('code')} name={p.get('project_name') or p.get('name')}")
        # members
        rm = requests.get(f'{BASE}/projects/{pid}/members', headers=ha)
        if rm.status_code == 200:
            members = rm.json().get('data', [])
            for m in members:
                print(f"    member: user_id={m.get('user_id')} role={m.get('role')} username={m.get('username','')}")
else:
    print('[projects]', r.status_code, r.text[:200])
