using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

public partial class CaseFlowDbContext : DbContext, IDataProtectionKeyContext
{
    public CaseFlowDbContext()
    {
    }

    public CaseFlowDbContext(DbContextOptions<CaseFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Case> Cases { get; set; }

    public virtual DbSet<CaseAssignment> CaseAssignments { get; set; }

    public virtual DbSet<CaseEstimation> CaseEstimations { get; set; }

    public virtual DbSet<CaseLog> CaseLogs { get; set; }

    public virtual DbSet<CaseReply> CaseReplies { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ProblemCategory> ProblemCategories { get; set; }

    public virtual DbSet<ProblemCategoryProject> ProblemCategoryProjects { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectCode> ProjectCodes { get; set; }

    public virtual DbSet<CaseType> CaseTypes { get; set; }

    public virtual DbSet<CaseTypeProject> CaseTypeProjects { get; set; }

    public virtual DbSet<ProblemCategoryCaseType> ProblemCategoryCaseTypes { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<SystemModule> SystemModules { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure the provider if no options have been provided (e.g. design-time scaffolding).
        if (!optionsBuilder.IsConfigured)
        {
            // Prefer environment variable for local development to avoid committing secrets.
            var conn = Environment.GetEnvironmentVariable("CASEFLOW_CONNECTION")
                ?? "Host=localhost;Database=CaseFlowDB;Username=postgres;Password=weaveres0404";
            optionsBuilder.UseNpgsql(conn);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.UseIdentityByDefaultColumns();

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("attachments_pkey");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_attach_entity").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.UploadedBy, "idx_attach_uploader").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.AttachmentId).UseIdentityAlwaysColumn();
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Attachments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("attachments_uploaded_by_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("audit_logs_pkey");

            entity.HasIndex(e => new { e.Action, e.CreatedAt }, "idx_audit_action").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CaseId, "idx_audit_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "idx_audit_entity").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_audit_user").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.AuditId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Case).WithMany(p => p.AuditLogs).HasConstraintName("audit_logs_case_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<Case>(entity =>
        {
            entity.HasKey(e => e.CaseId).HasName("cases_pkey");

            entity.HasIndex(e => e.AssignedPmId, "idx_cases_assigned_pm").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CategoryId, "idx_cases_category").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CreatedAt, "idx_cases_created_at").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CreatedBy, "idx_cases_created_by").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CustomerId, "idx_cases_customer").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Priority, "idx_cases_priority").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.ProjectId, e.CreatedAt }, "idx_cases_project_created").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.ProjectId, e.Status }, "idx_cases_project_status").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CaseType, "idx_cases_type").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.CaseId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CaseType).HasDefaultValueSql("'REPAIR'::character varying");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.OccurredAt).HasColumnType("timestamp without time zone");
            entity.Property(e => e.Priority).HasDefaultValueSql("'MEDIUM'::character varying");
            entity.Property(e => e.Status).HasDefaultValue((short)10);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.AssignedPm).WithMany(p => p.CaseAssignedPms).HasConstraintName("cases_assigned_pm_id_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.CaseCancelledByNavigations).HasConstraintName("cases_cancelled_by_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Cases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cases_category_id_fkey");

            entity.HasOne(d => d.ClosedByNavigation).WithMany(p => p.CaseClosedByNavigations).HasConstraintName("cases_closed_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CaseCreatedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cases_created_by_fkey");

            entity.HasOne(d => d.Customer).WithMany(p => p.Cases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cases_customer_id_fkey");

            entity.HasOne(d => d.Module).WithMany(p => p.Cases).HasConstraintName("cases_module_id_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.Cases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cases_project_id_fkey");

            entity.HasOne(d => d.RelatedCase).WithMany(p => p.InverseRelatedCase).HasConstraintName("cases_related_case_id_fkey");
        });

        modelBuilder.Entity<CaseAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("case_assignments_pkey");

            entity.HasIndex(e => e.AssignedBy, "idx_assign_by").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.CaseId, e.IsActive }, "idx_assign_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.SeUserId, e.IsActive }, "idx_assign_se").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.AssignmentId).UseIdentityAlwaysColumn();
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.CaseAssignmentAssignedByNavigations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_assignments_assigned_by_fkey");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_assignments_case_id_fkey");

            entity.HasOne(d => d.SeUser).WithMany(p => p.CaseAssignmentSeUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_assignments_se_user_id_fkey");
        });

        modelBuilder.Entity<CaseEstimation>(entity =>
        {
            entity.HasKey(e => e.EstimationId).HasName("case_estimations_pkey");

            entity.HasIndex(e => e.CaseId, "idx_est_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.EstimatorUserId, "idx_est_estimator").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.EstimationStatus, "idx_est_status").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.EstimationId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EstimationStatus).HasDefaultValue((short)10);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseEstimations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_estimations_case_id_fkey");

            entity.HasOne(d => d.EstimatorUser).WithMany(p => p.CaseEstimations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_estimations_estimator_user_id_fkey");
        });

        modelBuilder.Entity<CaseLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("case_logs_pkey");

            entity.HasIndex(e => e.CaseId, "idx_logs_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.LogDate, "idx_logs_date").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.HandlerUserId, e.LogDate }, "idx_logs_handler").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.LogId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Headcount).HasDefaultValue((short)1);
            entity.Property(e => e.LogDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.StatusAfter).HasDefaultValue((short)30);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_logs_case_id_fkey");

            entity.HasOne(d => d.HandlerUser).WithMany(p => p.CaseLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_logs_handler_user_id_fkey");
        });

        modelBuilder.Entity<CaseReply>(entity =>
        {
            entity.HasKey(e => e.ReplyId).HasName("case_replies_pkey");

            entity.HasIndex(e => e.CaseId, "idx_replies_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.ReplyId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ReplyDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseReplies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_replies_case_id_fkey");

            entity.HasOne(d => d.ReplierUser).WithMany(p => p.CaseReplies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("case_replies_replier_user_id_fkey");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("Customers_pkey");

            entity.HasIndex(e => e.IsActive, "idx_customers_active").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.CustomerId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.HasIndex(e => e.CaseId, "idx_notif_case").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.CreatedAt }, "idx_notif_recipient").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.NotificationId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Case).WithMany(p => p.Notifications).HasConstraintName("notifications_case_id_fkey");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_recipient_user_id_fkey");
        });

        modelBuilder.Entity<ProblemCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("problem_categories_pkey");

            entity.HasIndex(e => new { e.SortOrder, e.IsActive }, "idx_cat_sort").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("project_pkey");

            entity.HasIndex(e => e.IsActive, "idx_projects_active").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.CustomerId, "idx_projects_customer").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.ProjectId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Customer).WithMany(p => p.Projects)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_projects_customer");

            entity.HasOne(d => d.ProjectCodeRef).WithMany()
                .HasForeignKey(d => d.ProjectCodeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_projects_project_code");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("member_pkey");

            entity.HasIndex(e => new { e.ProjectId, e.MemberRole, e.IsActive }, "idx_pm_active").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "uk_project_user")
                .IsUnique()
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.MemberId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.MemberRole).HasDefaultValueSql("'SE'::character varying");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_member_projects");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_member_user");
        });

        modelBuilder.Entity<SystemModule>(entity =>
        {
            entity.HasKey(e => e.ModuleId).HasName("system_modules_pkey");

            entity.HasIndex(e => new { e.ProjectId, e.ModuleName }, "uk_project_module")
                .IsUnique()
                .HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Project).WithMany(p => p.SystemModules)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_system_modules_project");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Users_pkey");

            entity.HasIndex(e => e.IsActive, "idx_users_active").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.HasIndex(e => e.Role, "idx_users_role").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.UserId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Role).HasDefaultValueSql("'SE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.AuthProvider).HasDefaultValueSql("'local'::character varying");
            entity.HasIndex(e => e.GoogleSub)
                .IsUnique()
                .HasFilter("google_sub IS NOT NULL")
                .HasDatabaseName("idx_users_google_sub");
        });

        modelBuilder.Entity<ProjectCode>(entity =>
        {
            entity.HasKey(e => e.CodeId).HasName("project_codes_pkey");
            entity.HasIndex(e => e.Code, "uk_project_codes_code").IsUnique();
            entity.Property(e => e.CodeId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<ProblemCategoryProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_category_projects_pkey");
            entity.HasIndex(e => new { e.CategoryId, e.ProjectId }, "uk_cat_project").IsUnique();
            entity.HasIndex(e => e.CategoryId, "idx_cat_proj_category");
            entity.HasIndex(e => e.ProjectId, "idx_cat_proj_project");
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.HasOne(d => d.Category).WithMany(p => p.ProjectLinks)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Project).WithMany(p => p.CategoryLinks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CaseType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("case_types_pkey");
            entity.HasIndex(e => e.Code, "uk_case_types_code").IsUnique();
            entity.Property(e => e.TypeId).UseIdentityByDefaultColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<CaseTypeProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("case_type_projects_pkey");
            entity.HasIndex(e => new { e.TypeId, e.ProjectId }, "uk_ctype_project").IsUnique();
            entity.HasIndex(e => e.TypeId, "idx_ctype_proj_type");
            entity.HasIndex(e => e.ProjectId, "idx_ctype_proj_project");
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.HasOne(d => d.CaseType).WithMany(p => p.ProjectLinks)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Project).WithMany(p => p.CaseTypeLinks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProblemCategoryCaseType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_category_case_types_pkey");
            entity.HasIndex(e => new { e.CategoryId, e.TypeId }, "uk_cat_ctype").IsUnique();
            entity.HasIndex(e => e.CategoryId, "idx_cat_ctype_category");
            entity.HasIndex(e => e.TypeId, "idx_cat_ctype_type");
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.HasOne(d => d.Category).WithMany(p => p.CaseTypeLinks)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.CaseType).WithMany(p => p.CategoryLinks)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
