import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

cur.execute("SELECT category_id, category_name, sort_order FROM problem_categories WHERE case_type_filter = %s ORDER BY sort_order", ('MAINTENANCE',))
rows = cur.fetchall()
print("=== MAINTENANCE categories ===")
for r in rows:
    print(f"  id={r[0]}  name={r[1]}  sort={r[2]}")

conn.close()
