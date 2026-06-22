-- Normalize timestamp columns to the project rule:
--   timestamp without time zone, storing Asia/Taipei wall-clock values.
--
-- Run this only after backing up Cloud SQL.

SET TIME ZONE 'Asia/Taipei';

DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT table_schema, table_name, column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND data_type = 'timestamp with time zone'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I TYPE timestamp without time zone USING %I AT TIME ZONE %L',
            r.table_schema,
            r.table_name,
            r.column_name,
            r.column_name,
            'Asia/Taipei'
        );
    END LOOP;
END $$;

-- Optional verification:
--
-- SELECT table_name, column_name, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public'
--   AND data_type LIKE 'timestamp%'
-- ORDER BY table_name, column_name;
