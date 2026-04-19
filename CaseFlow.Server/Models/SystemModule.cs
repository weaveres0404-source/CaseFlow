using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Models;

[Table("system_modules")]
public partial class SystemModule
{
    [Key]
    [Column("module_id")]
    public int ModuleId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("module_name")]
    [StringLength(100)]
    public string ModuleName { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Module")]
    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    [ForeignKey("ProjectId")]
    [InverseProperty("SystemModules")]
    public virtual Project Project { get; set; } = null!;
}
