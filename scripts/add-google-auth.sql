-- =============================================================================
-- CaseFlow Migration: AddGoogleAuth
-- Migration ID : 20260521000000_AddGoogleAuth
-- Target       : PostgreSQL 16 / Cloud SQL
-- Safe         : Yes — uses IF NOT EXISTS / DO NOTHING
--
-- Changes:
--   1. users.google_sub    VARCHAR(100) NULL
--   2. users.google_email  VARCHAR(150) NULL
--   3. users.auth_provider VARCHAR(20) NOT NULL DEFAULT 'local'
--   4. Unique partial index idx_users_google_sub (google_sub) WHERE NOT NULL
--   5. Register migration in __EFMigrationsHistory
--
-- Prerequisites:
--   Run baseline-migrations.sql (and safe-migration.sql) before this.
--
-- Usage:
--   psql "host=HOST dbname=postgres user=postgres" -f add-google-auth.sql
-- Idempotent: Yes — safe to re-run.
-- =============================================================================

BEGIN;

-- 1. Add google_sub (nullable, stores Google's unique user identifier)
ALTER TABLE users ADD COLUMN IF NOT EXISTS google_sub VARCHAR(100);

-- 2. Add google_email (nullable, stores Google account email)
ALTER TABLE users ADD COLUMN IF NOT EXISTS google_email VARCHAR(150);

-- 3. Add auth_provider (NOT NULL with default 'local' for existing rows)
ALTER TABLE users ADD COLUMN IF NOT EXISTS auth_provider VARCHAR(20) NOT NULL DEFAULT 'local';

-- 4. Unique partial index on google_sub (only for non-null values)
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_google_sub
    ON users (google_sub)
    WHERE google_sub IS NOT NULL;

-- 5. Register migration in __EFMigrationsHistory
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260521000000_AddGoogleAuth', '10.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verification
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'users'
  AND column_name IN ('google_sub', 'google_email', 'auth_provider')
ORDER BY column_name;

SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'users'
  AND indexname = 'idx_users_google_sub';

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
