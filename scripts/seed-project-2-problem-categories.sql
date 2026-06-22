-- Seed project-2 problem categories and map them to project-specific case types.
-- Safe to run multiple times.

SET client_encoding TO 'UTF8';

BEGIN;

WITH category_seed(category_name, description, sort_order, case_type_code) AS (
  VALUES
    ('需求訪談',       '需求案件分類',     10, 'REQUEST'),
    ('需求確認/規格書', '需求案件分類',     20, 'REQUEST'),
    ('需求變更',       '需求案件分類',     30, 'REQUEST'),
    ('工時/需求評估',  '需求案件分類',     40, 'REQUEST'),
    ('功能開發',       '開發案件分類',     10, 'DEVELOPMENT'),
    ('AI模型/知識庫建置', '開發案件分類',   20, 'DEVELOPMENT'),
    ('系統分發',       '開發案件分類',     30, 'DEVELOPMENT'),
    ('環境建置',       '開發案件分類',     40, 'DEVELOPMENT'),
    ('測試案例設計',   '測試案件分類',     10, 'TEST'),
    ('內部測試',       '測試案件分類',     20, 'TEST'),
    ('缺陷修正/確認',  '測試案件分類',     30, 'TEST'),
    ('UAT環境準備',    'UAT案件分類',      10, 'UAT'),
    ('UAT測試支援',    'UAT案件分類',      20, 'UAT'),
    ('問題對應/修正',  'UAT案件分類',      30, 'UAT'),
    ('驗收確認',       'UAT案件分類',      40, 'UAT'),
    ('分區上線作業',   '上線導入案件分類', 10, 'GO_LIVE'),
    ('教育訓練',       '上線導入案件分類', 20, 'GO_LIVE'),
    ('上線後監控/問題對應', '上線導入案件分類', 30, 'GO_LIVE'),
    ('實體會議',       '會議案件分類',     10, 'MEETING'),
    ('線上會議',       '會議案件分類',     20, 'MEETING'),
    ('週報/月報製作',  '會議案件分類',     30, 'MEETING'),
    ('其他',           '其他案件分類',     90, 'TEST'),
    ('其他',           '其他案件分類',     90, 'UAT'),
    ('其他',           '其他案件分類',     90, 'GO_LIVE'),
    ('其他',           '其他案件分類',     90, 'MEETING'),
    ('其他',           '其他案件分類',     90, 'REQUEST'),
    ('其他',           '其他案件分類',     90, 'DEVELOPMENT')
),
category_rows AS (
  SELECT
    category_name,
    min(description) AS description,
    min(sort_order) AS sort_order
  FROM category_seed
  GROUP BY category_name
)
UPDATE public.problem_categories pc
SET
  description = cr.description,
  sort_order = cr.sort_order,
  project_id = COALESCE(pc.project_id, 2),
  is_active = TRUE,
  updated_at = now()
FROM category_rows cr
WHERE pc.category_name = cr.category_name;

WITH category_seed(category_name, description, sort_order, case_type_code) AS (
  VALUES
    ('需求訪談',       '需求案件分類',     10, 'REQUEST'),
    ('需求確認/規格書', '需求案件分類',     20, 'REQUEST'),
    ('需求變更',       '需求案件分類',     30, 'REQUEST'),
    ('工時/需求評估',  '需求案件分類',     40, 'REQUEST'),
    ('功能開發',       '開發案件分類',     10, 'DEVELOPMENT'),
    ('AI模型/知識庫建置', '開發案件分類',   20, 'DEVELOPMENT'),
    ('系統分發',       '開發案件分類',     30, 'DEVELOPMENT'),
    ('環境建置',       '開發案件分類',     40, 'DEVELOPMENT'),
    ('測試案例設計',   '測試案件分類',     10, 'TEST'),
    ('內部測試',       '測試案件分類',     20, 'TEST'),
    ('缺陷修正/確認',  '測試案件分類',     30, 'TEST'),
    ('UAT環境準備',    'UAT案件分類',      10, 'UAT'),
    ('UAT測試支援',    'UAT案件分類',      20, 'UAT'),
    ('問題對應/修正',  'UAT案件分類',      30, 'UAT'),
    ('驗收確認',       'UAT案件分類',      40, 'UAT'),
    ('分區上線作業',   '上線導入案件分類', 10, 'GO_LIVE'),
    ('教育訓練',       '上線導入案件分類', 20, 'GO_LIVE'),
    ('上線後監控/問題對應', '上線導入案件分類', 30, 'GO_LIVE'),
    ('實體會議',       '會議案件分類',     10, 'MEETING'),
    ('線上會議',       '會議案件分類',     20, 'MEETING'),
    ('週報/月報製作',  '會議案件分類',     30, 'MEETING'),
    ('其他',           '其他案件分類',     90, 'TEST'),
    ('其他',           '其他案件分類',     90, 'UAT'),
    ('其他',           '其他案件分類',     90, 'GO_LIVE'),
    ('其他',           '其他案件分類',     90, 'MEETING'),
    ('其他',           '其他案件分類',     90, 'REQUEST'),
    ('其他',           '其他案件分類',     90, 'DEVELOPMENT')
),
category_rows AS (
  SELECT
    category_name,
    min(description) AS description,
    min(sort_order) AS sort_order
  FROM category_seed
  GROUP BY category_name
)
INSERT INTO public.problem_categories (
  category_name,
  description,
  sort_order,
  case_type_filter,
  project_id,
  is_active,
  created_at,
  updated_at
)
SELECT
  category_name,
  description,
  sort_order,
  NULL,
  2,
  TRUE,
  now(),
  now()
FROM category_rows
WHERE NOT EXISTS (
  SELECT 1
  FROM public.problem_categories pc
  WHERE pc.category_name = category_rows.category_name
);

-- Project scope: make these categories available to project_id = 2.
-- If older data has duplicate category_name rows, use the smallest category_id as the canonical row.
WITH category_names(category_name) AS (
  SELECT DISTINCT category_name
  FROM (
    VALUES
      ('需求訪談'),
      ('需求確認/規格書'),
      ('需求變更'),
      ('工時/需求評估'),
      ('功能開發'),
      ('AI模型/知識庫建置'),
      ('系統分發'),
      ('環境建置'),
      ('測試案例設計'),
      ('內部測試'),
      ('缺陷修正/確認'),
      ('UAT環境準備'),
      ('UAT測試支援'),
      ('問題對應/修正'),
      ('驗收確認'),
      ('分區上線作業'),
      ('教育訓練'),
      ('上線後監控/問題對應'),
      ('實體會議'),
      ('線上會議'),
      ('週報/月報製作'),
      ('其他')
  ) AS v(category_name)
),
canonical_categories AS (
  SELECT min(pc.category_id) AS category_id, pc.category_name
  FROM public.problem_categories pc
  JOIN category_names cn ON cn.category_name = pc.category_name
  GROUP BY pc.category_name
)
INSERT INTO public.problem_category_projects (category_id, project_id)
SELECT cc.category_id, 2
FROM canonical_categories cc
WHERE EXISTS (SELECT 1 FROM public.projects p WHERE p.project_id = 2)
  AND NOT EXISTS (
    SELECT 1
    FROM public.problem_category_projects existing
    WHERE existing.category_id = cc.category_id
      AND existing.project_id = 2
  );

-- Case-type scope: map each category to its corresponding project-2 case type.
WITH category_seed(category_name, case_type_code) AS (
  VALUES
    ('需求訪談', 'REQUEST'),
    ('需求確認/規格書', 'REQUEST'),
    ('需求變更', 'REQUEST'),
    ('工時/需求評估', 'REQUEST'),
    ('其他', 'REQUEST'),
    ('功能開發', 'DEVELOPMENT'),
    ('AI模型/知識庫建置', 'DEVELOPMENT'),
    ('系統分發', 'DEVELOPMENT'),
    ('環境建置', 'DEVELOPMENT'),
    ('其他', 'DEVELOPMENT'),
    ('測試案例設計', 'TEST'),
    ('內部測試', 'TEST'),
    ('缺陷修正/確認', 'TEST'),
    ('其他', 'TEST'),
    ('UAT環境準備', 'UAT'),
    ('UAT測試支援', 'UAT'),
    ('問題對應/修正', 'UAT'),
    ('驗收確認', 'UAT'),
    ('其他', 'UAT'),
    ('分區上線作業', 'GO_LIVE'),
    ('教育訓練', 'GO_LIVE'),
    ('上線後監控/問題對應', 'GO_LIVE'),
    ('其他', 'GO_LIVE'),
    ('實體會議', 'MEETING'),
    ('線上會議', 'MEETING'),
    ('週報/月報製作', 'MEETING'),
    ('其他', 'MEETING')
),
canonical_categories AS (
  SELECT min(pc.category_id) AS category_id, pc.category_name
  FROM public.problem_categories pc
  JOIN (SELECT DISTINCT category_name FROM category_seed) csn ON csn.category_name = pc.category_name
  GROUP BY pc.category_name
)
INSERT INTO public.problem_category_case_types (category_id, type_id)
SELECT cc.category_id, ct.type_id
FROM category_seed cs
JOIN canonical_categories cc ON cc.category_name = cs.category_name
JOIN public.case_types ct ON ct.code = cs.case_type_code
WHERE NOT EXISTS (
  SELECT 1
  FROM public.problem_category_case_types existing
  WHERE existing.category_id = cc.category_id
    AND existing.type_id = ct.type_id
);

COMMIT;

SELECT
  ct.code AS case_type_code,
  ct.label AS case_type_label,
  pc.sort_order,
  pc.category_name
FROM public.problem_categories pc
JOIN public.problem_category_projects pcp ON pcp.category_id = pc.category_id
JOIN public.problem_category_case_types pcct ON pcct.category_id = pc.category_id
JOIN public.case_types ct ON ct.type_id = pcct.type_id
WHERE pcp.project_id = 2
  AND ct.code IN ('REQUEST', 'DEVELOPMENT', 'TEST', 'UAT', 'GO_LIVE', 'MEETING')
ORDER BY ct.sort_order, pc.sort_order, pc.category_id;
