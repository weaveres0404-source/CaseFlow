import requests, json

BASE = 'https://caseflow-test.sld-lwd.com'
r = requests.post(f'{BASE}/api/v1/auth/login', json={'username': 'luffy', 'password': '@sld123456'})
tok = r.json()['data']['access_token']
h = {'Authorization': f'Bearer {tok}'}

r = requests.get(f'{BASE}/api/v1/meta/dropdowns', headers=h)
d = r.json().get('data', {})
cats = d.get('categories', [])
case_types = d.get('case_types', [])

# Find MAINTENANCE type id
maint_id = next((t['id'] for t in case_types if t['code'] == 'MAINTENANCE'), None)
print(f'MAINTENANCE type_id={maint_id}')

# Filter MAINTENANCE categories
maint_cats = [c for c in cats if not c.get('case_type_ids') or maint_id in c.get('case_type_ids', [])]
print(f'\nMAINTENANCE categories ({len(maint_cats)}):')
for c in maint_cats:
    print(f'  id={c["id"]}  name={c["name"]}')

print(f'\nAll categories ({len(cats)}):')
for c in cats:
    print(f'  id={c["id"]}  name={c["name"]}  case_type_ids={c.get("case_type_ids")}')
