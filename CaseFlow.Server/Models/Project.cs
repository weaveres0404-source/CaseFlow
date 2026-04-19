using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("projects")]
public partial class Project
{
    [Key]
    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("project_code")]
    [StringLength(20)]
    public string ProjectCode { get; set; } = null!;

    [Column("project_name")]
    [StringLength(100)]
    public string ProjectName { get; set; } = null!;

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    [ForeignKey("CustomerId")]
    [InverseProperty("Projects")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Project")]
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    [InverseProperty("Project")]
    public virtual ICollection<SystemModule> SystemModules { get; set; } = new List<SystemModule>();
}
