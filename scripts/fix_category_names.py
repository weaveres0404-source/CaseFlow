import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

# Fix 1: PPC/工通知調查 → PP派工通知調查
cur.execute("""
    UPDATE problem_categories 
    SET category_name = 'PP派工通知調查', updated_at = NOW()
    WHERE category_name = 'PPC/工通知調查'
    RETURNING category_id, category_name
""")
r1 = cur.fetchall()
print(f'Fix1 (PPC→PP派): {len(r1)} rows updated', r1)

# Fix 2: 總部及應問題調查 → 總部應問題調查
cur.execute("""
    UPDATE problem_categories 
    SET category_name = '總部應問題調查', updated_at = NOW()
    WHERE category_name = '總部及應問題調查'
    RETURNING category_id, category_name
""")
r2 = cur.fetchall()
print(f'Fix2 (總部及應→總部應): {len(r2)} rows updated', r2)

conn.commit()
conn.close()
print('Done.')
