import requests, json
BASE='https://caseflow-test.sld-lwd.com'

# admin login
r=requests.post(f'{BASE}/api/v1/auth/login',json={'username':'admin','password':'@sld123456'})
tok=r.json()['data']['access_token']
ha={'Authorization':f'Bearer {tok}'}

# create case as sysadmin
r2=requests.post(f'{BASE}/api/v1/cases',json={'project_id':1,'title':'SysAdmin_perm_test3','description':'test','priority':'MEDIUM','hours_estimated':1,'category_id':1,'reporter_name':'tester','case_type':'REPAIR'},headers=ha)
cid=r2.json().get('data',{}).get('id',0)
print('create:', r2.status_code, 'case_id:', cid)

# nami login
rn=requests.post(f'{BASE}/api/v1/auth/login',json={'username':'nami','password':'@sld123456'})
nami_id=rn.json()['data']['user']['user_id']
nh={'Authorization':f'Bearer {rn.json()["data"]["access_token"]}'}

# sysadmin assign to nami
r4=requests.post(f'{BASE}/api/v1/cases/{cid}/assign',json={'se_user_id':nami_id,'hours_estimated':2},headers=ha)
print('assign_by_sysadmin:', r4.status_code)

# sysadmin return
r_ret=requests.post(f'{BASE}/api/v1/cases/{cid}/return',json={'reason':'sysadmin return test'},headers=ha)
print('return_by_sysadmin:', r_ret.status_code)

# reassign then sysadmin cancel
r4b=requests.post(f'{BASE}/api/v1/cases/{cid}/assign',json={'se_user_id':nami_id,'hours_estimated':2},headers=ha)
r5=requests.post(f'{BASE}/api/v1/cases/{cid}/cancel',json={'reason':'sysadmin cancel test'},headers=ha)
print('cancel_by_sysadmin:', r5.status_code)

print('ALL OK' if all(s in [200,201] for s in [r2.status_code,r4.status_code]) else 'SOME FAILED')
