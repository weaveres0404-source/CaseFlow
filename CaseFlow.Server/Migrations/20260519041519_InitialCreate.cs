using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CaseFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Customers_pkey", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "problem_categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    case_type_filter = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("problem_categories_pkey", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'SE'::character varying"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Users_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    project_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("project_pkey", x => x.project_id);
                    table.ForeignKey(
                        name: "fk_projects_customer",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "CustomerId");
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    attachment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stored_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_size = table.Column<int>(type: "integer", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    uploaded_by = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("attachments_pkey", x => x.attachment_id);
                    table.ForeignKey(
                        name: "attachments_uploaded_by_fkey",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    member_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    member_role = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'SE'::character varying"),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("member_pkey", x => x.member_id);
                    table.ForeignKey(
                        name: "fk_member_projects",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "fk_member_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "system_modules",
                columns: table => new
                {
                    module_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    module_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("system_modules_pkey", x => x.module_id);
                    table.ForeignKey(
                        name: "fk_system_modules_project",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "cases",
                columns: table => new
                {
                    case_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    case_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    module_id = table.Column<int>(type: "integer", nullable: true),
                    reporter_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reporter_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    case_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'REPAIR'::character varying"),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'MEDIUM'::character varying"),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    assigned_pm_id = table.Column<int>(type: "integer", nullable: true),
                    closed_by = table.Column<int>(type: "integer", nullable: true),
                    cancelled_by = table.Column<int>(type: "integer", nullable: true),
                    related_case_id = table.Column<int>(type: "integer", nullable: true),
                    relation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    total_hours = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cases_pkey", x => x.case_id);
                    table.ForeignKey(
                        name: "cases_assigned_pm_id_fkey",
                        column: x => x.assigned_pm_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "cases_cancelled_by_fkey",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "cases_category_id_fkey",
                        column: x => x.category_id,
                        principalTable: "problem_categories",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "cases_closed_by_fkey",
                        column: x => x.closed_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "cases_created_by_fkey",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "cases_customer_id_fkey",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "cases_module_id_fkey",
                        column: x => x.module_id,
                        principalTable: "system_modules",
                        principalColumn: "module_id");
                    table.ForeignKey(
                        name: "cases_project_id_fkey",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "cases_related_case_id_fkey",
                        column: x => x.related_case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    audit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entity_id = table.Column<int>(type: "integer", nullable: true),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_logs_pkey", x => x.audit_id);
                    table.ForeignKey(
                        name: "audit_logs_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "audit_logs_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "case_assignments",
                columns: table => new
                {
                    assignment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    se_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_by = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    expected_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("case_assignments_pkey", x => x.assignment_id);
                    table.ForeignKey(
                        name: "case_assignments_assigned_by_fkey",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "case_assignments_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "case_assignments_se_user_id_fkey",
                        column: x => x.se_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "case_estimations",
                columns: table => new
                {
                    estimation_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    estimator_user_id = table.Column<int>(type: "integer", nullable: false),
                    seq_no = table.Column<int>(type: "integer", nullable: false),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    estimated_hours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    reply_date = table.Column<DateOnly>(type: "date", nullable: true),
                    estimation_status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    case_log_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("case_estimations_pkey", x => x.estimation_id);
                    table.ForeignKey(
                        name: "case_estimations_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "case_estimations_estimator_user_id_fkey",
                        column: x => x.estimator_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "case_logs",
                columns: table => new
                {
                    log_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    handler_user_id = table.Column<int>(type: "integer", nullable: false),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    handling_method = table.Column<string>(type: "text", nullable: false),
                    handling_result = table.Column<string>(type: "text", nullable: true),
                    hours_spent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    headcount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    status_after = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)30),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("case_logs_pkey", x => x.log_id);
                    table.ForeignKey(
                        name: "case_logs_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "case_logs_handler_user_id_fkey",
                        column: x => x.handler_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "case_replies",
                columns: table => new
                {
                    reply_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    replier_user_id = table.Column<int>(type: "integer", nullable: false),
                    reply_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    reply_content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("case_replies_pkey", x => x.reply_id);
                    table.ForeignKey(
                        name: "case_replies_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "case_replies_replier_user_id_fkey",
                        column: x => x.replier_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    recipient_user_id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.notification_id);
                    table.ForeignKey(
                        name: "notifications_case_id_fkey",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "case_id");
                    table.ForeignKey(
                        name: "notifications_recipient_user_id_fkey",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_attach_entity",
                table: "attachments",
                columns: new[] { "entity_type", "entity_id" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_attach_uploader",
                table: "attachments",
                column: "uploaded_by")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_audit_action",
                table: "audit_logs",
                columns: new[] { "action", "created_at" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_audit_case",
                table: "audit_logs",
                column: "case_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_audit_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_audit_user",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_assign_by",
                table: "case_assignments",
                column: "assigned_by")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_assign_case",
                table: "case_assignments",
                columns: new[] { "case_id", "is_active" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_assign_se",
                table: "case_assignments",
                columns: new[] { "se_user_id", "is_active" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_est_case",
                table: "case_estimations",
                column: "case_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_est_estimator",
                table: "case_estimations",
                column: "estimator_user_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_est_status",
                table: "case_estimations",
                column: "estimation_status")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_logs_case",
                table: "case_logs",
                column: "case_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_logs_date",
                table: "case_logs",
                column: "log_date")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_logs_handler",
                table: "case_logs",
                columns: new[] { "handler_user_id", "log_date" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_replies_case",
                table: "case_replies",
                column: "case_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_case_replies_replier_user_id",
                table: "case_replies",
                column: "replier_user_id");

            migrationBuilder.CreateIndex(
                name: "cases_case_number_key",
                table: "cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cases_assigned_pm",
                table: "cases",
                column: "assigned_pm_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_category",
                table: "cases",
                column: "category_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_created_at",
                table: "cases",
                column: "created_at")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_created_by",
                table: "cases",
                column: "created_by")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_customer",
                table: "cases",
                column: "customer_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_priority",
                table: "cases",
                column: "priority")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_project_created",
                table: "cases",
                columns: new[] { "project_id", "created_at" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_project_status",
                table: "cases",
                columns: new[] { "project_id", "status" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cases_type",
                table: "cases",
                column: "case_type")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_cases_cancelled_by",
                table: "cases",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_cases_closed_by",
                table: "cases",
                column: "closed_by");

            migrationBuilder.CreateIndex(
                name: "IX_cases_module_id",
                table: "cases",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_cases_related_case_id",
                table: "cases",
                column: "related_case_id");

            migrationBuilder.CreateIndex(
                name: "idx_customers_active",
                table: "customers",
                column: "is_active")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_notif_case",
                table: "notifications",
                column: "case_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_notif_recipient",
                table: "notifications",
                columns: new[] { "recipient_user_id", "is_read", "created_at" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_cat_sort",
                table: "problem_categories",
                columns: new[] { "sort_order", "is_active" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "uk_problem_categories_name",
                table: "problem_categories",
                column: "category_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pm_active",
                table: "project_members",
                columns: new[] { "project_id", "member_role", "is_active" })
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "IX_project_members_user_id",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_project_user",
                table: "project_members",
                columns: new[] { "project_id", "user_id" },
                unique: true)
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_projects_active",
                table: "projects",
                column: "is_active")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_projects_customer",
                table: "projects",
                column: "customer_id")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "uk_project_module",
                table: "system_modules",
                columns: new[] { "project_id", "module_name" },
                unique: true)
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_users_active",
                table: "users",
                column: "is_active")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");

            migrationBuilder.CreateIndex(
                name: "idx_users_role",
                table: "users",
                column: "role")
                .Annotation("Npgsql:StorageParameter:deduplicate_items", "true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "case_assignments");

            migrationBuilder.DropTable(
                name: "case_estimations");

            migrationBuilder.DropTable(
                name: "case_logs");

            migrationBuilder.DropTable(
                name: "case_replies");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "cases");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "problem_categories");

            migrationBuilder.DropTable(
                name: "system_modules");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
