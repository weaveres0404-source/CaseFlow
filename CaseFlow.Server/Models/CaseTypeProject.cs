using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.Server.Models;

[Table("case_type_projects")]
public partial class CaseTypeProject
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("type_id")]
    public int TypeId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [ForeignKey("TypeId")]
    public virtual CaseType CaseType { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; } = null!;
}
