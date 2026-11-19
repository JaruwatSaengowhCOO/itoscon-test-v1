using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class Division
{
    public int Id { get; set; }

    [Display(Name = "field.divisionName")]
    [LRequired(ErrorMessage = "err.divisionName")]
    [LStringLength(100, ErrorMessage = "err.divisionName")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Department> Departments { get; set; } = new List<Department>();
}