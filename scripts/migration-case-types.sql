-- Migration: case_types lookup + case_type_projects + problem_category_case_types
-- Run on local and production

-- 1. case_types 對照表
CREATE TABLE IF NOT EXISTS case_types (
    type_id     SERIAL PRIMARY KEY,
    code        VARCHAR(20) NOT NULL,
    label       VARCHAR(100) NOT NULL,
    description TEXT,
    color       VARCHAR(50),
    sort_order  INT NOT NULL DEFAULT 0,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMP NOT NULL DEFAULT now(),
    updated_at  TIMESTAMP NOT NULL DEFAULT now(),
    CONSTRAINT uk_case_types_code UNIQUE (code)
);

-- 2. case_type ↔ project 多對多
CREATE TABLE IF NOT EXISTS case_type_projects (
    id          SERIAL PRIMARY KEY,
    type_id     INT NOT NULL REFERENCES case_types(type_id) ON DELETE CASCADE,
    project_id  INT NOT NULL REFERENCES projects(project_id) ON DELETE CASCADE,
    CONSTRAINT uk_ctype_project UNIQUE (type_id, project_id)
);
CREATE INDEX IF NOT EXISTS idx_ctype_proj_type    ON case_type_projects(type_id);
CREATE INDEX IF NOT EXISTS idx_ctype_proj_project ON case_type_projects(project_id);

-- 3. case_type ↔ problem_category 多對多
CREATE TABLE IF NOT EXISTS problem_category_case_types (
    id          SERIAL PRIMARY KEY,
    category_id INT NOT NULL REFERENCES problem_categories(category_id) ON DELETE CASCADE,
    type_id     INT NOT NULL REFERENCES case_types(type_id) ON DELETE CASCADE,
    CONSTRAINT uk_cat_ctype UNIQUE (category_id, type_id)
);
CREATE INDEX IF NOT EXISTS idx_cat_ctype_category ON problem_category_case_types(category_id);
CREATE INDEX IF NOT EXISTS idx_cat_ctype_type     ON problem_category_case_types(type_id);

-- 4. 種子既有 5 種案件類型（與 enum 對應）
INSERT INTO case_types (code, label, sort_order)
VALUES
  ('REPAIR',      '障礙調查', 10),
  ('EVALUATION',  '工時評估', 20),
  ('MAINTENANCE', '日常維運', 30),
  ('UHD',         'UHD協助', 40),
  ('INQUIRY',     '一般詢問', 50)
ON CONFLICT (code) DO NOTHING;

UPDATE case_types
SET color = CASE code
    WHEN 'REPAIR' THEN 'bg-red-100 text-red-800'
    WHEN 'EVALUATION' THEN 'bg-purple-100 text-purple-800'
    WHEN 'MAINTENANCE' THEN 'bg-blue-100 text-blue-800'
    WHEN 'UHD' THEN 'bg-teal-100 text-teal-800'
    WHEN 'INQUIRY' THEN 'bg-sky-100 text-sky-800'
    ELSE color
END,
updated_at = now()
WHERE code IN ('REPAIR', 'EVALUATION', 'MAINTENANCE', 'UHD', 'INQUIRY');

-- 5. 由 projects.allowed_case_types CSV 回填 case_type_projects
DO $$
DECLARE
    p RECORD;
    code_tok TEXT;
BEGIN
    FOR p IN SELECT project_id, allowed_case_types FROM projects
             WHERE allowed_case_types IS NOT NULL AND length(trim(allowed_case_types)) > 0
    LOOP
        FOR code_tok IN SELECT trim(t) FROM regexp_split_to_table(p.allowed_case_types, ',') AS t
        LOOP
            IF code_tok <> '' THEN
                INSERT INTO case_type_projects (type_id, project_id)
                SELECT ct.type_id, p.project_id
                FROM case_types ct
                WHERE ct.code = code_tok
                ON CONFLICT DO NOTHING;
            END IF;
        END LOOP;
    END LOOP;
END $$;

-- 6. 由 problem_categories.case_type_filter 回填 problem_category_case_types
INSERT INTO problem_category_case_types (category_id, type_id)
SELECT pc.category_id, ct.type_id
FROM problem_categories pc
JOIN case_types ct ON ct.code = pc.case_type_filter
WHERE pc.case_type_filter IS NOT NULL AND length(trim(pc.case_type_filter)) > 0
ON CONFLICT DO NOTHING;

SELECT 'Migration OK: case_types + junctions' AS result;
