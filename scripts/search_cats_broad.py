import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

# Search more broadly
cur.execute("""
    SELECT category_id, category_name 
    FROM problem_categories 
    WHERE category_name LIKE %s OR category_name LIKE %s 
       OR category_name LIKE %s OR category_name LIKE %s
""", ('%PP%', '%工通知%', '%總部%', '%例行%'))
rows = cur.fetchall()
print('Found:', len(rows))
for r in rows:
    print(f'  id={r[0]}  name={r[1]}  hex={r[1].encode("utf-8").hex()[:40]}')

conn.close()
