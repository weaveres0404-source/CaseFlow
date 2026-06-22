import psycopg2

conn = psycopg2.connect(host='localhost', database='CaseFlowDB', user='postgres', password='weaveres0404')
cur = conn.cursor()

# 查全部分類，不管 case_type_filter，包括可能有不同 project_id 的
cur.execute("""
    SELECT category_id, category_name, case_type_filter, parent_type, project_id, sort_order, is_active
    FROM problem_categories
    ORDER BY case_type_filter NULLS LAST, sort_order
""")
rows = cur.fetchall()
print(f"Total: {len(rows)} categories")
for r in rows:
    print(f"  id={r[0]:3d}  name={r[1]:30s}  type={str(r[2]):12s}  parent={str(r[3]):12s}  proj={r[4]}  sort={r[5]}  active={r[6]}")

conn.close()
