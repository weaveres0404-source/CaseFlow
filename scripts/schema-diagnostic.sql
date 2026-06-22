-- =============================================================================
-- CaseFlow Schema Diagnostic
-- Source  : EF Core migration 20260519041519_InitialCreate (exact column names)
-- Target  : PostgreSQL 16 / Cloud SQL
-- Safe    : Read-only — zero DDL, zero DML
--
-- Usage   : psql "host=HOST dbname=postgres user=postgres" \
--                -v ON_ERROR_STOP=0 -f schema-diagnostic.sql
--
-- Result  : §2, §3, §4 returning 0 rows → schema matches InitialCreate.
-- =============================================================================

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§1  EF Core Migration History'
\echo '══════════════════════════════════════════════════════════════'

SELECT
  CASE
    WHEN EXISTS (
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = 'public'
        AND table_name   = '__EFMigrationsHistory'
    )
    THEN 'EXISTS  ✓'
    ELSE 'MISSING ✗  → run baseline-migrations.sql'
  END AS "__EFMigrationsHistory";

DO $$
DECLARE r RECORD;
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema='public' AND table_name='__EFMigrationsHistory'
  ) THEN
    RAISE NOTICE '__EFMigrationsHistory not found – skipping migration list';
    RETURN;
  END IF;
  FOR r IN SELECT "MigrationId", "ProductVersion"
           FROM "__EFMigrationsHistory"
           ORDER BY "MigrationId"
  LOOP
    RAISE NOTICE 'Applied: %  (EF %)', r."MigrationId", r."ProductVersion";
  END LOOP;
END $$;

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§2  Missing Tables  (rows = problem → run safe-migration.sql)'
\echo '══════════════════════════════════════════════════════════════'

WITH expected(tbl) AS (VALUES
  ('customers'),('problem_categories'),('users'),('projects'),
  ('attachments'),('project_members'),('system_modules'),('cases'),
  ('audit_logs'),('case_assignments'),('case_estimations'),
  ('case_logs'),('case_replies'),('notifications')
)
SELECT e.tbl AS missing_table
FROM expected e
WHERE NOT EXISTS (
  SELECT 1 FROM information_schema.tables t
  WHERE  t.table_schema = 'public' AND t.table_name = e.tbl
);

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§3  Missing Columns  (rows = problem → run safe-migration.sql)'
\echo '══════════════════════════════════════════════════════════════'
\echo 'NOTE: customers PK is "CustomerId" (EF PascalCase, quoted identifier)'

WITH expected(tbl, col) AS (VALUES
  -- customers  — PK is "CustomerId" (mixed-case, quoted in PG)
  ('customers','CustomerId'),
  ('customers','customer_name'),
  ('customers','contact_person'),
  ('customers','contact_phone'),
  ('customers','contact_email'),
  ('customers','address'),
  ('customers','remarks'),
  ('customers','is_active'),
  ('customers','created_at'),
  ('customers','updated_at'),
  -- problem_categories
  ('problem_categories','category_id'),
  ('problem_categories','category_name'),
  ('problem_categories','description'),
  ('problem_categories','sort_order'),
  ('problem_categories','case_type_filter'),
  ('problem_categories','is_active'),
  ('problem_categories','created_at'),
  ('problem_categories','updated_at'),
  -- users  (must_change_password added in v3.0 – critical)
  ('users','user_id'),
  ('users','username'),
  ('users','password_hash'),
  ('users','full_name'),
  ('users','email'),
  ('users','phone'),
  ('users','role'),
  ('users','is_active'),
  ('users','last_login_at'),
  ('users','created_at'),
  ('users','updated_at'),
  ('users','must_change_password'),
  -- projects
  ('projects','project_id'),
  ('projects','project_code'),
  ('projects','project_name'),
  ('projects','customer_id'),
  ('projects','description'),
  ('projects','start_date'),
  ('projects','end_date'),
  ('projects','is_active'),
  ('projects','created_at'),
  ('projects','updated_at'),
  -- attachments  (attachment_id = integer IDENTITY ALWAYS)
  ('attachments','attachment_id'),
  ('attachments','file_name'),
  ('attachments','stored_name'),
  ('attachments','file_path'),
  ('attachments','file_size'),
  ('attachments','mime_type'),
  ('attachments','entity_type'),
  ('attachments','entity_id'),
  ('attachments','uploaded_by'),
  ('attachments','uploaded_at'),
  -- project_members  (joined_at = date, member_role = varchar no-length)
  ('project_members','member_id'),
  ('project_members','project_id'),
  ('project_members','user_id'),
  ('project_members','member_role'),
  ('project_members','joined_at'),
  ('project_members','is_active'),
  ('project_members','created_at'),
  -- system_modules
  ('system_modules','module_id'),
  ('system_modules','project_id'),
  ('system_modules','module_name'),
  ('system_modules','description'),
  ('system_modules','is_active'),
  ('system_modules','created_at'),
  ('system_modules','updated_at'),
  -- cases  (case_id = integer IDENTITY ALWAYS, total_hours = numeric(7,2))
  ('cases','case_id'),
  ('cases','case_number'),
  ('cases','project_id'),
  ('cases','customer_id'),
  ('cases','category_id'),
  ('cases','module_id'),
  ('cases','reporter_name'),
  ('cases','reporter_phone'),
  ('cases','reporter_email'),
  ('cases','case_type'),
  ('cases','priority'),
  ('cases','description'),
  ('cases','status'),
  ('cases','created_by'),
  ('cases','assigned_pm_id'),
  ('cases','closed_by'),
  ('cases','cancelled_by'),
  ('cases','related_case_id'),
  ('cases','relation_type'),
  ('cases','closed_at'),
  ('cases','cancelled_at'),
  ('cases','created_at'),
  ('cases','updated_at'),
  ('cases','total_hours'),
  -- audit_logs  (audit_id = bigint IDENTITY ALWAYS)
  ('audit_logs','audit_id'),
  ('audit_logs','user_id'),
  ('audit_logs','case_id'),
  ('audit_logs','action'),
  ('audit_logs','entity_type'),
  ('audit_logs','entity_id'),
  ('audit_logs','old_value'),
  ('audit_logs','new_value'),
  ('audit_logs','ip_address'),
  ('audit_logs','user_agent'),
  ('audit_logs','created_at'),
  -- case_assignments
  ('case_assignments','assignment_id'),
  ('case_assignments','case_id'),
  ('case_assignments','se_user_id'),
  ('case_assignments','assigned_by'),
  ('case_assignments','is_primary'),
  ('case_assignments','instructions'),
  ('case_assignments','expected_completion_date'),
  ('case_assignments','is_active'),
  ('case_assignments','assigned_at'),
  ('case_assignments','created_at'),
  -- case_estimations  (case_log_id = integer nullable, no FK in migration)
  ('case_estimations','estimation_id'),
  ('case_estimations','case_id'),
  ('case_estimations','estimator_user_id'),
  ('case_estimations','seq_no'),
  ('case_estimations','request_date'),
  ('case_estimations','summary'),
  ('case_estimations','estimated_hours'),
  ('case_estimations','reply_date'),
  ('case_estimations','estimation_status'),
  ('case_estimations','remarks'),
  ('case_estimations','created_at'),
  ('case_estimations','updated_at'),
  ('case_estimations','case_log_id'),
  -- case_logs
  ('case_logs','log_id'),
  ('case_logs','case_id'),
  ('case_logs','handler_user_id'),
  ('case_logs','log_date'),
  ('case_logs','handling_method'),
  ('case_logs','handling_result'),
  ('case_logs','hours_spent'),
  ('case_logs','headcount'),
  ('case_logs','status_after'),
  ('case_logs','created_at'),
  ('case_logs','updated_at'),
  -- case_replies
  ('case_replies','reply_id'),
  ('case_replies','case_id'),
  ('case_replies','replier_user_id'),
  ('case_replies','reply_date'),
  ('case_replies','reply_content'),
  ('case_replies','created_at'),
  ('case_replies','updated_at'),
  -- notifications
  ('notifications','notification_id'),
  ('notifications','recipient_user_id'),
  ('notifications','case_id'),
  ('notifications','notification_type'),
  ('notifications','title'),
  ('notifications','message'),
  ('notifications','is_read'),
  ('notifications','read_at'),
  ('notifications','created_at')
)
SELECT e.tbl AS table_name, e.col AS missing_column
FROM expected e
WHERE NOT EXISTS (
  SELECT 1 FROM information_schema.columns c
  WHERE  c.table_schema = 'public'
    AND  c.table_name   = e.tbl
    AND  c.column_name  = e.col   -- exact case-sensitive match
)
ORDER BY e.tbl, e.col;

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§4  Missing Indexes  (rows = missing → run safe-migration.sql)'
\echo '══════════════════════════════════════════════════════════════'

WITH expected_idx(idx_name) AS (VALUES
  ('idx_attach_entity'),('idx_attach_uploader'),
  ('idx_audit_action'),('idx_audit_case'),('idx_audit_entity'),('idx_audit_user'),
  ('idx_assign_by'),('idx_assign_case'),('idx_assign_se'),
  ('idx_est_case'),('idx_est_estimator'),('idx_est_status'),
  ('idx_logs_case'),('idx_logs_date'),('idx_logs_handler'),
  ('idx_replies_case'),('IX_case_replies_replier_user_id'),
  ('cases_case_number_key'),
  ('idx_cases_assigned_pm'),('idx_cases_category'),('idx_cases_created_at'),
  ('idx_cases_created_by'),('idx_cases_customer'),('idx_cases_priority'),
  ('idx_cases_project_created'),('idx_cases_project_status'),('idx_cases_type'),
  ('IX_cases_cancelled_by'),('IX_cases_closed_by'),
  ('IX_cases_module_id'),('IX_cases_related_case_id'),
  ('idx_customers_active'),
  ('idx_notif_case'),('idx_notif_recipient'),
  ('idx_cat_sort'),('uk_problem_categories_name'),
  ('idx_pm_active'),('IX_project_members_user_id'),('uk_project_user'),
  ('idx_projects_active'),('idx_projects_customer'),
  ('uk_project_module'),
  ('idx_users_active'),('idx_users_role')
)
SELECT e.idx_name AS missing_index
FROM expected_idx e
WHERE NOT EXISTS (
  SELECT 1 FROM pg_indexes pi
  WHERE  pi.schemaname = 'public' AND pi.indexname = e.idx_name
);

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§5  Key Column Type Spot-Check'
\echo '══════════════════════════════════════════════════════════════'

SELECT
  c.table_name,
  c.column_name,
  c.udt_name                         AS pg_type,
  c.character_maximum_length,
  c.numeric_precision,
  c.numeric_scale,
  c.is_nullable,
  left(c.column_default, 50)         AS col_default
FROM information_schema.columns c
WHERE c.table_schema = 'public'
  AND (
    (c.table_name = 'customers'        AND c.column_name = 'CustomerId')
 OR (c.table_name = 'users'            AND c.column_name IN ('must_change_password','role','email'))
 OR (c.table_name = 'cases'            AND c.column_name IN ('case_id','status','total_hours','category_id'))
 OR (c.table_name = 'attachments'      AND c.column_name IN ('attachment_id','file_size'))
 OR (c.table_name = 'audit_logs'       AND c.column_name = 'audit_id')
 OR (c.table_name = 'project_members'  AND c.column_name IN ('member_role','joined_at'))
 OR (c.table_name = 'case_estimations' AND c.column_name = 'case_log_id')
  )
ORDER BY c.table_name, c.ordinal_position;

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§6  Row Counts (data sanity)'
\echo '══════════════════════════════════════════════════════════════'

SELECT
  t.table_name,
  (xpath('/row/c/text()',
    query_to_xml(
      format('SELECT COUNT(*) AS c FROM %I', t.table_name),
      false, true, ''
    )
  ))[1]::text::bigint AS row_count
FROM information_schema.tables t
WHERE t.table_schema = 'public'
  AND t.table_name IN (
    'customers','problem_categories','users','projects','attachments',
    'project_members','system_modules','cases','audit_logs',
    'case_assignments','case_estimations','case_logs','case_replies','notifications'
  )
ORDER BY t.table_name;

\echo ''
\echo '══════════════════════════════════════════════════════════════'
\echo '§7  Sequence Last Values'
\echo '══════════════════════════════════════════════════════════════'

SELECT sequence_name, last_value, is_called
FROM pg_sequences
WHERE schemaname = 'public'
ORDER BY sequence_name;

\echo ''
\echo 'Diagnostic complete.'
\echo '§2 and §3 returning 0 rows = schema matches InitialCreate.'
