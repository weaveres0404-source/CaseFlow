import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

cur.execute("SELECT category_id, category_name, case_type_filter, parent_type, sort_order FROM problem_categories ORDER BY case_type_filter, sort_order")
rows = cur.fetchall()
for r in rows:
    print(f"  id={r[0]}  name={r[1]}  type={r[2]}  parent={r[3]}  sort={r[4]}")

conn.close()
