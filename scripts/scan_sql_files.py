import os, glob

search_terms = ['PPC', '例行作業', '總部及應', '客服反應', '系統優化調整']
sql_files = glob.glob(r'D:\CaseFlow\CaseFlow\*.sql') + glob.glob(r'D:\CaseFlow\CaseFlow\scripts\*.sql')

for f in sql_files:
    try:
        with open(f, encoding='utf-8', errors='replace') as fp:
            content = fp.read()
        found = [t for t in search_terms if t in content]
        if found:
            print(f'\nFOUND {found} in: {os.path.basename(f)}')
            for i, line in enumerate(content.splitlines(), 1):
                if any(t in line for t in found):
                    print(f'  L{i}: {line[:120]}')
        else:
            print(f'OK (clean): {os.path.basename(f)}')
    except Exception as e:
        print(f'ERROR {os.path.basename(f)}: {e}')
