-- Migration: project-scoped case types and categories
-- Run once on production DB

-- 1. Projects: 允許的案件類型（逗號分隔，NULL 表示全部類型皆可用）
ALTER TABLE projects
    ADD COLUMN IF NOT EXISTS allowed_case_types VARCHAR(100) NULL;

COMMENT ON COLUMN projects.allowed_case_types IS
    'Comma-separated allowed case type values (e.g. ''REPAIR,EVALUATION''). NULL = all types allowed.';

-- 2. Problem categories: 歸屬專案（NULL 表示所有專案共用）
ALTER TABLE problem_categories
    ADD COLUMN IF NOT EXISTS project_id INTEGER NULL
        REFERENCES projects(project_id) ON DELETE SET NULL;

COMMENT ON COLUMN problem_categories.project_id IS
    'Project-specific category. NULL = available across all projects.';

-- (Optional) 若需要允許不同專案有同名分類，可把全域 unique 改為 partial：
-- DROP INDEX IF EXISTS uk_problem_categories_name;
-- CREATE UNIQUE INDEX uk_problem_categories_name
--     ON problem_categories (category_name)
--     WHERE project_id IS NULL;
