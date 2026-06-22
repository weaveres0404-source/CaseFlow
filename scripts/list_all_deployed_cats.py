import requests, json

BASE = 'https://caseflow-test.sld-lwd.com'
r = requests.post(f'{BASE}/api/v1/auth/login', json={'username': 'admin', 'password': '@sld123456'})
tok = r.json()['data']['access_token']
h = {'Authorization': f'Bearer {tok}'}

# try multiple pages
for pg in [1, 2, 3]:
    r = requests.get(f'{BASE}/api/v1/problem-categories?page={pg}&page_size=50', headers=h)
    d = r.json().get('data', [])
    if not d:
        break
    print(f'Page {pg}: {len(d)} items')
    for x in d:
        iid = x.get('id') or x.get('category_id')
        name = x.get('name') or x.get('category_name')
        ctype = x.get('case_type_filter')
        active = x.get('is_active', True)
        print(f'  id={iid}  name={name}  type={ctype}  active={active}')
