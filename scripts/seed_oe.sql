INSERT INTO project_codes (code, label, description, sort_order, is_active, created_at, updated_at)
VALUES ('OE', 'OO電子商務平台', '下週一上線示範專案', 10, true, now(), now())
ON CONFLICT (code) DO UPDATE SET label=EXCLUDED.label, is_active=true, updated_at=now()
RETURNING code_id, code;

WITH new_code AS (SELECT code_id FROM project_codes WHERE code='OE')
INSERT INTO projects (project_code, project_code_id, project_name, customer_id, description, start_date, is_active, created_at, updated_at)
SELECT 'OE', new_code.code_id, 'OO電子商務平台', 1, '下週一上線示範專案', CURRENT_DATE + INTERVAL '4 days', true, now(), now()
FROM new_code
WHERE NOT EXISTS (SELECT 1 FROM projects WHERE project_code='OE')
RETURNING project_id, project_code, project_code_id, project_name;
