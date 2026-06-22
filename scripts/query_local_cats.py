import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

# 查詢本機 MAINTENANCE 分類 (case_type_filter = MAINTENANCE)
cur.execute("SELECT id, name, sort_order FROM problem_categories WHERE case_type_filter = 'MAINTENANCE' ORDER BY sort_order, id")
rows = cur.fetchall()
print("=== MAINTENANCE categories in local DB ===")
for r in rows:
    print(f"  id={r[0]} name={r[1]} sort={r[2]}")

conn.close()
