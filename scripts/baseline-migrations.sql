-- =============================================================================
-- CaseFlow Baseline Migrations Script
-- Source  : EF Core migration 20260519041519_InitialCreate
-- Target  : PostgreSQL 16 / Cloud SQL
-- Safe    : No DDL on application tables — only touches __EFMigrationsHistory
--
-- Purpose : Register the InitialCreate migration in __EFMigrationsHistory
--           WITHOUT re-running it (because the tables already exist).
--           After this script, "dotnet ef database update" will skip
--           InitialCreate and only apply future migrations.
--
-- Prerequisites:
--   1. Run safe-migration.sql first to ensure all tables and columns exist.
--   2. Then run this script ONCE.
--   3. After this, EF tooling (dotnet ef database update) is safe to use.
--
-- Usage   : psql "host=HOST dbname=postgres user=postgres" -f baseline-migrations.sql
-- Idempotent: Yes — ON CONFLICT DO NOTHING makes it safe to re-run.
-- =============================================================================

BEGIN;

-- Create __EFMigrationsHistory if it does not yet exist.
-- Column sizes match what EF Core expects exactly:
--   MigrationId   varchar(150)
--   ProductVersion varchar(32)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    CHARACTER VARYING(150) NOT NULL,
    "ProductVersion" CHARACTER VARYING(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Register InitialCreate as applied.
-- ON CONFLICT DO NOTHING → safe to run multiple times.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260519041519_InitialCreate', '10.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verification
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
