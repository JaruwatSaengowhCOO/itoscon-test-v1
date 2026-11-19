using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class Team
{
    public int Id { get; set; }

    [Display(Name = "field.teamName")]
    [LRequired(ErrorMessage = "err.teamName")]
    [LStringLength(100, ErrorMessage = "err.teamName")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "field.department")]
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}