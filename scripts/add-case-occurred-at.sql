-- =============================================================================
-- CaseFlow Migration: AddCaseOccurredAt
-- Migration ID : 20260709000000_AddCaseOccurredAt
-- Target       : PostgreSQL 16 / Cloud SQL
-- Safe         : Yes — uses IF NOT EXISTS / ON CONFLICT DO NOTHING
--
-- Changes:
--   1. cases.occurred_at TIMESTAMP (without time zone) NULL
--      案件實際發生時間（補件用）。可為 NULL；未填時應用程式以 created_at 帶入。
--      與其它時間欄位一致，以 UTC 儲存於 timestamp without time zone。
--   2. Register migration in __EFMigrationsHistory
--
-- Usage:
--   psql "host=HOST dbname=postgres user=postgres" -f add-case-occurred-at.sql
-- Idempotent: Yes — safe to re-run.
-- =============================================================================

BEGIN;

-- 1. Add occurred_at (nullable, UTC)
ALTER TABLE cases ADD COLUMN IF NOT EXISTS occurred_at TIMESTAMP WITHOUT TIME ZONE;

-- 2. Register migration in __EFMigrationsHistory
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260709000000_AddCaseOccurredAt', '10.0.6')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verification
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'cases'
  AND column_name = 'occurred_at';
