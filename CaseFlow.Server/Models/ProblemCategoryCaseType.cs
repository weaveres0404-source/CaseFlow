using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaseFlow.Server.Models;

[Table("problem_category_case_types")]
public partial class ProblemCategoryCaseType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("type_id")]
    public int TypeId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual ProblemCategory Category { get; set; } = null!;

    [ForeignKey("TypeId")]
    public virtual CaseType CaseType { get; set; } = null!;
}
