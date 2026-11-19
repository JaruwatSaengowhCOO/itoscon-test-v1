using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class SubRole
{
    public int Id { get; set; }

    [Display(Name = "field.subRoleName")]
    [LRequired(ErrorMessage = "err.subRoleName")]
    [LStringLength(100, ErrorMessage = "err.subRoleName")]
    public string Name { get; set; } = string.Empty;

    // บทบาทที่ความสัมพันธ์นี้เป็นของ (ถ้ายังไม่ผูกเป็น null)
    [Display(Name = "field.linkedRole")]
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
}