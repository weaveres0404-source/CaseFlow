import requests, json

BASE = 'https://caseflow-test.sld-lwd.com'
r = requests.post(f'{BASE}/api/v1/auth/login', json={'username': 'admin', 'password': '@sld123456'})
tok = r.json()['data']['access_token']
h = {'Authorization': f'Bearer {tok}'}

r = requests.get(f'{BASE}/api/v1/problem-categories?page_size=200', headers=h)
d = r.json().get('data', [])
print(f'Total: {len(d)}')
for x in d:
    iid = x.get('id') or x.get('category_id')
    name = x.get('name') or x.get('category_name')
    ctype = x.get('case_type_filter')
    print(f'  id={iid}  name={name}  type={ctype}')
