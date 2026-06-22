using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Server.Models;

namespace CaseFlow.Server.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "SysAdmin")]
public class AdminController : ControllerBase
{
    private readonly CaseFlowDbContext _db;
    private readonly ILogger<AdminController> _logger;

    private static readonly (string Table, string Column)[] _expectedColumns =
    [
        ("users", "must_change_password"),
        ("cases", "total_hours"),
        ("case_estimations", "case_log_id"),
        ("customers", "customer_id"), ("customers", "customer_name"),
        ("problem_categories", "category_id"), ("problem_categories", "case_type_filter"),
        ("projects", "project_id"), ("projects", "project_code"),
        ("attachments", "attachment_id"), ("attachments", "entity_type"),
        ("project_members", "member_id"),
        ("system_modules", "module_id"),
        ("cases", "case_id"), ("cases", "case_number"), ("cases", "status"),
        ("cases", "assigned_pm_id"), ("cases", "closed_by"), ("cases", "cancelled_by"),
        ("audit_logs", "audit_id"),
        ("case_assignments", "assignment_id"), ("case_assignments", "is_primary"),
        ("case_estimations", "estimation_id"), ("case_estimations", "estimation_status"),
        ("case_logs", "log_id"), ("case_logs", "status_after"),
        ("case_replies", "reply_id"),
        ("notifications", "notification_id"),
    ];

    private static readonly string[] _expectedTables =
    [
        "customers", "problem_categories", "users", "projects", "attachments",
        "project_members", "system_modules", "cases", "audit_logs",
        "case_assignments", "case_estimations", "case_logs", "case_replies", "notifications"
    ];

    public AdminController(CaseFlowDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/admin/schema-check
    /// Returns a JSON report of:
    ///   - Whether __EFMigrationsHistory exists and which migrations are applied
    ///   - Which expected tables are missing
    ///   - Which expected columns are missing
    ///   - Row counts for all main tables
    /// </summary>
    [HttpGet("schema-check")]
    public async Task<IActionResult> SchemaCheck()
    {
        try
        {
            var report = new SchemaReport();

            await using var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync();

            // 1. Migrations history
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = '__EFMigrationsHistory'
                    )";
                report.MigrationsHistoryTableExists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
            }

            if (report.MigrationsHistoryTableExists)
            {
                await using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = @"SELECT ""MigrationId"", ""ProductVersion"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId""";
                await using var r2 = await cmd2.ExecuteReaderAsync();
                while (await r2.ReadAsync())
                    report.AppliedMigrations.Add(new AppliedMigration(r2.GetString(0), r2.GetString(1)));
            }

            // 2. Existing tables
            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd3 = conn.CreateCommand())
            {
                cmd3.CommandText = @"
                    SELECT table_name FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
                await using var r3 = await cmd3.ExecuteReaderAsync();
                while (await r3.ReadAsync())
                    existingTables.Add(r3.GetString(0));
            }
            report.MissingTables = _expectedTables.Where(t => !existingTables.Contains(t)).ToList();

            // 3. Existing columns
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd4 = conn.CreateCommand())
            {
                cmd4.CommandText = @"
                    SELECT table_name || '.' || column_name
                    FROM information_schema.columns
                    WHERE table_schema = 'public'";
                await using var r4 = await cmd4.ExecuteReaderAsync();
                while (await r4.ReadAsync())
                    existingCols.Add(r4.GetString(0));
            }
            report.MissingColumns = _expectedColumns
                .Where(c => !existingCols.Contains($"{c.Table}.{c.Column}"))
                .Select(c => $"{c.Table}.{c.Column}")
                .ToList();

            // 4. Row counts
            foreach (var tbl in _expectedTables.Where(existingTables.Contains))
            {
                await using var cmd5 = conn.CreateCommand();
                cmd5.CommandText = $"SELECT COUNT(*) FROM {tbl}";
                var cnt = (long)(await cmd5.ExecuteScalarAsync() ?? 0L);
                report.RowCounts[tbl] = cnt;
            }

            report.SchemaOk = report.MissingTables.Count == 0 && report.MissingColumns.Count == 0;
            report.Recommendation = BuildRecommendation(report);

            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema check failed");
            return StatusCode(500, new { success = false, error = new { code = "SCHEMA_CHECK_FAILED", message = ex.Message } });
        }
    }

    private static string BuildRecommendation(SchemaReport r)
    {
        if (r.SchemaOk && r.MigrationsHistoryTableExists && r.AppliedMigrations.Any(m => m.MigrationId == "20260519041519_InitialCreate"))
            return "Schema is fully consistent with EF Core migrations. No action required.";
        if (!r.MigrationsHistoryTableExists)
            return "Run scripts/safe-migration.sql first, then scripts/baseline-migrations.sql to register the InitialCreate migration.";
        if (r.MissingTables.Count > 0 || r.MissingColumns.Count > 0)
            return "Run scripts/safe-migration.sql to add missing tables/columns (idempotent, no data loss).";
        if (!r.AppliedMigrations.Any(m => m.MigrationId == "20260519041519_InitialCreate"))
            return "Tables exist but InitialCreate is not in history. Run scripts/baseline-migrations.sql.";
        return "Unknown state — review the report manually.";
    }

    public record AppliedMigration(string MigrationId, string ProductVersion);

    public class SchemaReport
    {
        public bool SchemaOk { get; set; }
        public bool MigrationsHistoryTableExists { get; set; }
        public List<AppliedMigration> AppliedMigrations { get; set; } = [];
        public List<string> MissingTables { get; set; } = [];
        public List<string> MissingColumns { get; set; } = [];
        public Dictionary<string, long> RowCounts { get; set; } = new();
        public string Recommendation { get; set; } = "";
    }
}
