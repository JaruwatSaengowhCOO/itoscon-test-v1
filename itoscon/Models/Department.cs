using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class Department
{
    public int Id { get; set; }

    [Display(Name = "field.departmentName")]
    [LRequired(ErrorMessage = "err.departmentName")]
    [LStringLength(100, ErrorMessage = "err.departmentName")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "field.division")]
    public int DivisionId { get; set; }
    public Division Division { get; set; } = null!;

    public ICollection<Team> Teams { get; set; } = new List<Team>();
}