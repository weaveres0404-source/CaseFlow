-- Migration: project_codes + problem_category_projects
-- Run on local and production

-- 1. project_codes 對照表
CREATE TABLE IF NOT EXISTS project_codes (
    code_id     SERIAL PRIMARY KEY,
    code        VARCHAR(10) NOT NULL,  -- 短編號（2 碼為主，例: SM、BK、OE）
    label       VARCHAR(100) NOT NULL,
    description TEXT,
    sort_order  INT NOT NULL DEFAULT 0,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMP NOT NULL DEFAULT now(),
    updated_at  TIMESTAMP NOT NULL DEFAULT now(),
    CONSTRAINT uk_project_codes_code UNIQUE (code)
);

-- projects.project_code_id is required by the current EF model.
-- Existing databases may only have projects.project_code, so keep this additive.
ALTER TABLE projects
    ADD COLUMN IF NOT EXISTS project_code_id INT NULL;

INSERT INTO project_codes (code, label, description, sort_order, is_active, created_at, updated_at)
SELECT DISTINCT
    LEFT(project_code, 10),
    project_code,
    NULL,
    0,
    TRUE,
    now(),
    now()
FROM projects
WHERE project_code IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM project_codes pc
      WHERE pc.code = LEFT(projects.project_code, 10)
  );

UPDATE projects p
SET project_code_id = pc.code_id
FROM project_codes pc
WHERE p.project_code_id IS NULL
  AND pc.code = LEFT(p.project_code, 10);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_projects_project_code'
    ) THEN
        ALTER TABLE projects
            ADD CONSTRAINT fk_projects_project_code
            FOREIGN KEY (project_code_id)
            REFERENCES project_codes(code_id)
            ON DELETE SET NULL;
    END IF;
END $$;

-- 2. problem_category_projects 多對多關聯表
CREATE TABLE IF NOT EXISTS problem_category_projects (
    id          SERIAL PRIMARY KEY,
    category_id INT NOT NULL REFERENCES problem_categories(category_id) ON DELETE CASCADE,
    project_id  INT NOT NULL REFERENCES projects(project_id) ON DELETE CASCADE,
    CONSTRAINT uk_cat_project UNIQUE (category_id, project_id)
);
CREATE INDEX IF NOT EXISTS idx_cat_proj_category ON problem_category_projects(category_id);
CREATE INDEX IF NOT EXISTS idx_cat_proj_project  ON problem_category_projects(project_id);

-- 3. 遷移現有 problem_categories.project_id => junction table
INSERT INTO problem_category_projects (category_id, project_id)
SELECT category_id, project_id
FROM   problem_categories
WHERE  project_id IS NOT NULL
ON CONFLICT DO NOTHING;

SELECT 'Migration OK' AS result;
