"""
診斷 user_id=2 無法看到專案的根因
1. 確認 auth/login 回傳的 user.user_id 型別
2. 確認 meta/dropdowns 的 project_members[].user_id 型別
3. 模擬前端 filter 邏輯
"""
import requests, json

BASE = 'https://caseflow-test.sld-lwd.com/api/v1'

# 試各帳號，找出哪個帳號是 user_id=2
candidates = ['luffy','zoro','conan','nami','sasuke','goku','admin']
found_user = None
for uname in candidates:
    r = requests.post(f'{BASE}/auth/login', json={'username': uname, 'password': '@sld123456'})
    if r.status_code == 200 and r.json().get('success'):
        u = r.json()['data']['user']
        uid = u.get('user_id') or u.get('id')
        if uid == 2:
            print(f'user_id=2 is: {uname} / full_name={u.get("full_name","")}, role={u.get("role","")}')
            found_user = (uname, r.json()['data']['access_token'], u)
            break
        else:
            print(f'  {uname}: user_id={uid}')

if not found_user:
    print('Could not find user_id=2 among test accounts')
    print('Trying with "screenshot user" - need username from screenshot (江鎮宇)')
else:
    uname, tok, user_info = found_user
    h = {'Authorization': f'Bearer {tok}'}
    print(f'\nauth.user = {json.dumps(user_info, ensure_ascii=False)}')
    print(f'auth.user.user_id type = {type(user_info.get("user_id")).__name__}')

    # Get meta/dropdowns
    r2 = requests.get(f'{BASE}/meta/dropdowns', headers=h)
    data = r2.json().get('data', {})
    pms = data.get('project_members', [])
    projects = data.get('projects', [])
    
    print(f'\nproject_members total: {len(pms)}')
    # Simulate frontend filter
    userId = user_info.get('user_id')
    print(f'Filtering with userId={userId} (type={type(userId).__name__})')
    my_pm = [pm for pm in pms if pm.get('user_id') == userId and pm.get('role') == 'PM']
    print(f'My PM project_members: {my_pm}')
    myPMProjectIds = set(pm['project_id'] for pm in my_pm)
    available = [p for p in projects if p['id'] in myPMProjectIds]
    print(f'Available projects: {[p["name"] for p in available]}')
    
    # Check type mismatch
    for pm in pms[:5]:
        uid_in_pm = pm.get('user_id')
        print(f'  pm entry: user_id={uid_in_pm}({type(uid_in_pm).__name__}) project_id={pm.get("project_id")} role={pm.get("role")}')
