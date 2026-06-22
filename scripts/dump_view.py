import json
d=json.load(open('scripts/scenarios.json',encoding='utf8'))
for s in d:
    if not str(s.get('id','')).startswith('TC-'): continue
    print(f"{s['id']:8s} [{s.get('category','')}] {s.get('name','')}")
    steps=(s.get('steps','') or '').replace('\n',' | ')[:220]
    ui=(s.get('expectedUI','') or '').replace('\n',' ')[:160]
    print(f'         steps: {steps}')
    print(f'         expUI: {ui}')
