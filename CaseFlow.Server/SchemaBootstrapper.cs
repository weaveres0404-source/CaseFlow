using Microsoft.EntityFrameworkCore;
using Npgsql;
using CaseFlow.Server.Models;

namespace CaseFlow.Server;

/// <summary>
/// Read-only startup diagnostic: compares the actual Cloud SQL schema against
/// what the EF Core InitialCreate migration expects, and logs any discrepancies
/// to stderr (Cloud Run logs).  Does NOT perform any DDL.
/// </summary>
public static class SchemaBootstrapper
{
    // Every (table, column) pair that InitialCreate creates.
    private static readonly (string Table, string Column, string Notes)[] ExpectedColumns =
    [
        ("customers",           "customer_id",              "PK"),
        ("customers",           "customer_name",            ""),
        ("customers",           "contact_person",           "nullable"),
        ("customers",           "contact_phone",            "nullable"),
        ("customers",           "contact_email",            "nullable"),
        ("customers",           "address",                  "nullable"),
        ("customers",           "remarks",                  "nullable"),
        ("customers",           "is_active",                ""),
        ("customers",           "created_at",               ""),
        ("customers",           "updated_at",               ""),

        ("problem_categories",  "category_id",              "PK"),
        ("problem_categories",  "category_name",            ""),
        ("problem_categories",  "description",              "nullable"),
        ("problem_categories",  "sort_order",               ""),
        ("problem_categories",  "case_type_filter",         "nullable"),
        ("problem_categories",  "project_id",               "nullable FK"),
        ("problem_categories",  "is_active",                ""),
        ("problem_categories",  "created_at",               ""),
        ("problem_categories",  "updated_at",               ""),

        ("users",               "user_id",                  "PK"),
        ("users",               "username",                 ""),
        ("users",               "password_hash",            ""),
        ("users",               "full_name",                ""),
        ("users",               "email",                    "nullable"),
        ("users",               "phone",                    "nullable"),
        ("users",               "role",                     ""),
        ("users",               "is_active",                ""),
        ("users",               "last_login_at",            "nullable"),
        ("users",               "created_at",               ""),
        ("users",               "updated_at",               ""),
        ("users",               "must_change_password",     "default FALSE"),

        ("projects",            "project_id",               "PK"),
        ("projects",            "project_code",             ""),
        ("projects",            "project_name",             ""),
        ("projects",            "customer_id",              "FK"),
        ("projects",            "description",              "nullable"),
        ("projects",            "start_date",               "nullable"),
        ("projects",            "end_date",                 "nullable"),
        ("projects",            "is_active",                ""),
        ("projects",            "allowed_case_types",        "nullable"),
        ("projects",            "created_at",               ""),
        ("projects",            "updated_at",               ""),

        ("attachments",         "attachment_id",            "PK IDENTITY"),
        ("attachments",         "file_name",                ""),
        ("attachments",         "stored_name",              ""),
        ("attachments",         "file_path",                ""),
        ("attachments",         "file_size",                ""),
        ("attachments",         "mime_type",                "nullable"),
        ("attachments",         "entity_type",              ""),
        ("attachments",         "entity_id",                ""),
        ("attachments",         "uploaded_by",              "FK"),
        ("attachments",         "uploaded_at",              ""),

        ("project_members",     "member_id",                "PK"),
        ("project_members",     "project_id",               "FK"),
        ("project_members",     "user_id",                  "FK"),
        ("project_members",     "member_role",              "nullable"),
        ("project_members",     "joined_at",                ""),
        ("project_members",     "is_active",                ""),
        ("project_members",     "created_at",               ""),

        ("system_modules",      "module_id",                "PK"),
        ("system_modules",      "project_id",               "FK"),
        ("system_modules",      "module_name",              ""),
        ("system_modules",      "description",              "nullable"),
        ("system_modules",      "is_active",                ""),
        ("system_modules",      "created_at",               ""),
        ("system_modules",      "updated_at",               ""),

        ("cases",               "case_id",                  "PK IDENTITY"),
        ("cases",               "case_number",              "UNIQUE"),
        ("cases",               "project_id",               "FK"),
        ("cases",               "customer_id",              "FK"),
        ("cases",               "category_id",              "FK nullable"),
        ("cases",               "module_id",                "FK nullable"),
        ("cases",               "reporter_name",            "nullable"),
        ("cases",               "reporter_phone",           "nullable"),
        ("cases",               "reporter_email",           "nullable"),
        ("cases",               "case_type",                ""),
        ("cases",               "priority",                 ""),
        ("cases",               "description",              ""),
        ("cases",               "status",                   "smallint default 10"),
        ("cases",               "created_by",               "FK"),
        ("cases",               "assigned_pm_id",           "FK nullable"),
        ("cases",               "closed_by",                "FK nullable"),
        ("cases",               "cancelled_by",             "FK nullable"),
        ("cases",               "related_case_id",          "FK nullable"),
        ("cases",               "relation_type",            "nullable"),
        ("cases",               "closed_at",                "nullable"),
        ("cases",               "cancelled_at",             "nullable"),
        ("cases",               "created_at",               ""),
        ("cases",               "updated_at",               ""),
        ("cases",               "total_hours",              "numeric(7,2)"),

        ("audit_logs",          "audit_id",                 "PK IDENTITY bigint"),
        ("audit_logs",          "user_id",                  "FK"),
        ("audit_logs",          "case_id",                  "FK nullable"),
        ("audit_logs",          "action",                   ""),
        ("audit_logs",          "entity_type",              "nullable"),
        ("audit_logs",          "entity_id",                "nullable"),
        ("audit_logs",          "old_value",                "jsonb nullable"),
        ("audit_logs",          "new_value",                "jsonb nullable"),
        ("audit_logs",          "ip_address",               "nullable"),
        ("audit_logs",          "user_agent",               "nullable"),
        ("audit_logs",          "created_at",               ""),

        ("case_assignments",    "assignment_id",            "PK IDENTITY"),
        ("case_assignments",    "case_id",                  "FK"),
        ("case_assignments",    "se_user_id",               "FK"),
        ("case_assignments",    "assigned_by",              "FK"),
        ("case_assignments",    "is_primary",               ""),
        ("case_assignments",    "instructions",             "nullable"),
        ("case_assignments",    "expected_completion_date", "date nullable"),
        ("case_assignments",    "is_active",                "default true"),
        ("case_assignments",    "assigned_at",              ""),
        ("case_assignments",    "created_at",               ""),

        ("case_estimations",    "estimation_id",            "PK IDENTITY"),
        ("case_estimations",    "case_id",                  "FK"),
        ("case_estimations",    "estimator_user_id",        "FK"),
        ("case_estimations",    "seq_no",                   ""),
        ("case_estimations",    "request_date",             "date"),
        ("case_estimations",    "summary",                  ""),
        ("case_estimations",    "estimated_hours",          "numeric(6,2)"),
        ("case_estimations",    "reply_date",               "date nullable"),
        ("case_estimations",    "estimation_status",        "smallint default 10"),
        ("case_estimations",    "remarks",                  "nullable"),
        ("case_estimations",    "created_at",               ""),
        ("case_estimations",    "updated_at",               ""),
        ("case_estimations",    "case_log_id",              "nullable"),

        ("case_logs",           "log_id",                   "PK IDENTITY"),
        ("case_logs",           "case_id",                  "FK"),
        ("case_logs",           "handler_user_id",          "FK"),
        ("case_logs",           "log_date",                 "date default CURRENT_DATE"),
        ("case_logs",           "handling_method",          ""),
        ("case_logs",           "handling_result",          "nullable"),
        ("case_logs",           "hours_spent",              "numeric(6,2)"),
        ("case_logs",           "headcount",                "smallint default 1"),
        ("case_logs",           "status_after",             "smallint default 30"),
        ("case_logs",           "created_at",               ""),
        ("case_logs",           "updated_at",               ""),

        ("case_replies",        "reply_id",                 "PK IDENTITY"),
        ("case_replies",        "case_id",                  "FK"),
        ("case_replies",        "replier_user_id",          "FK"),
        ("case_replies",        "reply_date",               "date default CURRENT_DATE"),
        ("case_replies",        "reply_content",            ""),
        ("case_replies",        "created_at",               ""),
        ("case_replies",        "updated_at",               ""),

        ("notifications",       "notification_id",          "PK IDENTITY"),
        ("notifications",       "recipient_user_id",        "FK"),
        ("notifications",       "case_id",                  "FK nullable"),
        ("notifications",       "notification_type",        ""),
        ("notifications",       "title",                    ""),
        ("notifications",       "message",                  ""),
        ("notifications",       "is_read",                  ""),
        ("notifications",       "read_at",                  "nullable"),
        ("notifications",       "created_at",               ""),
    ];

    private static readonly string[] ExpectedTables =
    [
        "customers", "problem_categories", "users", "projects", "attachments",
        "project_members", "system_modules", "cases", "audit_logs",
        "case_assignments", "case_estimations", "case_logs", "case_replies", "notifications"
    ];

    public static async Task RunDiagnosticAsync(CaseFlowDbContext db, ILogger logger)
    {
        try
        {
            // 1. Check DB connectivity
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("[SCHEMA] Cannot connect to database. Check connection string.");
                return;
            }
            logger.LogInformation("[SCHEMA] Database connection OK.");

            // 使用獨立的 NpgsqlConnection，避免 dispose 到 EF Core 管理的連接
            var connStr = db.Database.GetConnectionString()!;
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // 2. Check __EFMigrationsHistory
            const string historyQuery = @"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = '__EFMigrationsHistory'
                ) AS history_exists;";

            var historyExists = false;
            await using (var sqlCmd = conn.CreateCommand())
            {
                sqlCmd.CommandText = historyQuery;
                var result = await sqlCmd.ExecuteScalarAsync();
                historyExists = result is bool b && b;
            }

            if (!historyExists)
            {
                logger.LogWarning("[SCHEMA] __EFMigrationsHistory table does NOT exist. " +
                    "Run scripts/baseline-migrations.sql to register InitialCreate as applied.");
            }
            else
            {
                const string migQuery = @"
                    SELECT ""MigrationId"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId"";";
                var migrations = new List<string>();
                await using (var sqlCmd2 = conn.CreateCommand())
                {
                    sqlCmd2.CommandText = migQuery;
                    await using var reader = await sqlCmd2.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        migrations.Add(reader.GetString(0));
                }

                if (migrations.Contains("20260519041519_InitialCreate"))
                    logger.LogInformation("[SCHEMA] InitialCreate migration is registered in history.");
                else
                    logger.LogWarning("[SCHEMA] InitialCreate migration is NOT in __EFMigrationsHistory. " +
                        "Run scripts/baseline-migrations.sql before using 'dotnet ef database update'.");

                foreach (var m in migrations)
                    logger.LogInformation("[SCHEMA] Applied migration: {Migration}", m);
            }

            // 3. Check each expected table
            const string tableQuery = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                ORDER BY table_name;";

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var sqlCmd3 = conn.CreateCommand())
            {
                sqlCmd3.CommandText = tableQuery;
                await using var reader3 = await sqlCmd3.ExecuteReaderAsync();
                while (await reader3.ReadAsync())
                    existingTables.Add(reader3.GetString(0));
            }

            var missingTables = ExpectedTables.Where(t => !existingTables.Contains(t)).ToList();
            if (missingTables.Count > 0)
                logger.LogError("[SCHEMA] MISSING TABLES: {Tables}. Run scripts/safe-migration.sql to create them.",
                    string.Join(", ", missingTables));
            else
                logger.LogInformation("[SCHEMA] All {Count} expected tables found.", ExpectedTables.Length);

            // 4. Check each expected column
            const string colQuery = @"
                SELECT table_name, column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                ORDER BY table_name, ordinal_position;";

            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var sqlCmd4 = conn.CreateCommand())
            {
                sqlCmd4.CommandText = colQuery;
                await using var reader4 = await sqlCmd4.ExecuteReaderAsync();
                while (await reader4.ReadAsync())
                    existingCols.Add($"{reader4.GetString(0)}.{reader4.GetString(1)}");
            }

            var missingCols = ExpectedColumns
                .Where(c => !existingCols.Contains($"{c.Table}.{c.Column}"))
                .ToList();

            if (missingCols.Count > 0)
            {
                logger.LogError("[SCHEMA] {Count} MISSING COLUMN(S) detected:", missingCols.Count);
                foreach (var c in missingCols)
                    logger.LogError("[SCHEMA]   MISSING: {Table}.{Column}  ({Notes})", c.Table, c.Column, c.Notes);
                logger.LogError("[SCHEMA] Run scripts/safe-migration.sql to add missing columns.");
            }
            else
            {
                logger.LogInformation("[SCHEMA] All expected columns are present. Schema looks good.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SCHEMA] Diagnostic failed with exception: {Message}", ex.Message);
        }
    }
}
