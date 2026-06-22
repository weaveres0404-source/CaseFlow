using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.Server.Models;

[Table("problem_category_projects")]
public partial class ProblemCategoryProject
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual ProblemCategory Category { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; } = null!;
}
