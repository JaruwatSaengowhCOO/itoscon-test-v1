using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class Role
{
    public int Id { get; set; }

    [Display(Name = "field.roleName")]
    [LRequired(ErrorMessage = "err.roleName")]
    [LStringLength(100, ErrorMessage = "err.roleName")]
    public string Name { get; set; } = string.Empty;

    public ICollection<SubRole> SubRoles { get; set; } = new List<SubRole>();
}