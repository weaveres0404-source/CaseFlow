import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

cur.execute("""
    SELECT * FROM problem_categories 
    WHERE category_name LIKE %s OR category_name LIKE %s 
       OR category_name LIKE %s OR category_name LIKE %s 
       OR category_name LIKE %s
""", ('%PPC%', '%例行%', '%總部%', '%客服%', '%系統優化%'))
rows = cur.fetchall()
print('Matching rows:', len(rows))
for r in rows:
    print(r)

cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name")
tables = [r[0] for r in cur.fetchall()]
print('\nTables:', tables)
conn.close()
