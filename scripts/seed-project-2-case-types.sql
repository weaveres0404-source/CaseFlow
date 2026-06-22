-- Seed project-scoped case types.
-- Project 1 keeps the existing case types; project 2 uses the AI customer-service flow types.
-- Safe to run multiple times.

SET client_encoding TO 'UTF8';

INSERT INTO public.case_types (code, label, sort_order, color)
VALUES
  ('REQUEST',     '需求',     10, 'bg-indigo-100 text-indigo-800'),
  ('DEVELOPMENT', '開發',     20, 'bg-blue-100 text-blue-800'),
  ('TEST',        '測試',     30, 'bg-amber-100 text-amber-800'),
  ('UAT',         'UAT',      40, 'bg-purple-100 text-purple-800'),
  ('GO_LIVE',     '上線導入', 50, 'bg-emerald-100 text-emerald-800'),
  ('MEETING',     '會議',     60, 'bg-slate-100 text-slate-700')
ON CONFLICT (code) DO UPDATE
SET
  label = EXCLUDED.label,
  sort_order = EXCLUDED.sort_order,
  color = EXCLUDED.color,
  is_active = TRUE,
  updated_at = now();

-- Existing project-1 case types observed in the current system.
-- Linking them to project 1 prevents them from appearing as "global" options for project 2.
WITH legacy_types(code) AS (
  VALUES
    ('REQUEST'),
    ('UHD'),
    ('MAINT'),
    ('MAINTENANCE'),
    ('SUPPORT'),
    ('INCIDENT'),
    ('MEETING'),
    ('RELEASE'),
    ('UPGRADE'),
    ('REPAIR'),
    ('EVALUATION'),
    ('INQUIRY')
)
INSERT INTO public.case_type_projects (type_id, project_id)
SELECT ct.type_id, 1
FROM public.case_types ct
JOIN legacy_types lt ON lt.code = ct.code
WHERE EXISTS (SELECT 1 FROM public.projects p WHERE p.project_id = 1)
ON CONFLICT DO NOTHING;

-- Project 2 button options: 需求 / 開發 / 測試 / UAT / 上線導入 / 會議
WITH project2_types(code) AS (
  VALUES
    ('REQUEST'),
    ('DEVELOPMENT'),
    ('TEST'),
    ('UAT'),
    ('GO_LIVE'),
    ('MEETING')
)
INSERT INTO public.case_type_projects (type_id, project_id)
SELECT ct.type_id, 2
FROM public.case_types ct
JOIN project2_types p2t ON p2t.code = ct.code
WHERE EXISTS (SELECT 1 FROM public.projects p WHERE p.project_id = 2)
ON CONFLICT DO NOTHING;

-- Keep the legacy CSV field in sync for older admin/API screens.
UPDATE public.projects
SET
  allowed_case_types = 'REQUEST,DEVELOPMENT,TEST,UAT,GO_LIVE,MEETING',
  updated_at = now()
WHERE project_id = 2;

-- Allow both legacy and project-2 case type codes at the cases table level.
-- Application validation still enforces which codes each project may use.
ALTER TABLE public.cases
  DROP CONSTRAINT IF EXISTS cases_case_type_check;

ALTER TABLE public.cases
  ADD CONSTRAINT cases_case_type_check
  CHECK (
    (case_type)::text = ANY (
      ARRAY[
        ('REQUEST'::character varying)::text,
        ('DEVELOPMENT'::character varying)::text,
        ('TEST'::character varying)::text,
        ('UAT'::character varying)::text,
        ('GO_LIVE'::character varying)::text,
        ('MEETING'::character varying)::text,
        ('UHD'::character varying)::text,
        ('MAINT'::character varying)::text,
        ('SUPPORT'::character varying)::text,
        ('INCIDENT'::character varying)::text,
        ('RELEASE'::character varying)::text,
        ('UPGRADE'::character varying)::text,
        ('REPAIR'::character varying)::text,
        ('EVALUATION'::character varying)::text,
        ('MAINTENANCE'::character varying)::text,
        ('INQUIRY'::character varying)::text
      ]
    )
  );

SELECT
  ct.type_id,
  ct.code,
  ct.label,
  array_agg(ctp.project_id ORDER BY ctp.project_id) AS project_ids
FROM public.case_types ct
LEFT JOIN public.case_type_projects ctp ON ctp.type_id = ct.type_id
WHERE ct.code IN ('REQUEST', 'DEVELOPMENT', 'TEST', 'UAT', 'GO_LIVE', 'MEETING')
GROUP BY ct.type_id, ct.code, ct.label
ORDER BY ct.sort_order, ct.type_id;
