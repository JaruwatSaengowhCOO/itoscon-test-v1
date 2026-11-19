using System.ComponentModel.DataAnnotations;
using itoscon.Services;

namespace itoscon.Models;

public class Contact
{
    public int Id { get; set; }

    [Display(Name = "field.firstName")]
    [LRequired(ErrorMessage = "err.firstNameRequired")]
    [LStringLength(100, ErrorMessage = "err.firstNameLength")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "field.lastName")]
    [LRequired(ErrorMessage = "err.lastNameRequired")]
    [LStringLength(100, ErrorMessage = "err.lastNameLength")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "field.phone")]
    [LRequired(ErrorMessage = "err.phoneRequired")]
    [LRegularExpression(@"^0\d{8,9}$", ErrorMessage = "err.phoneFormat")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "field.email")]
    [LRequired(ErrorMessage = "err.emailRequired")]
    [LEmail(ErrorMessage = "err.emailFormat")]
    [LStringLength(200, ErrorMessage = "err.emailFormat")]
    public string Email { get; set; } = string.Empty;

    // เก็บเป็น hash เสมอ ความยาวของรหัสผ่านดิบตรวจใน controller
    [Display(Name = "field.password")]
    [LRequired(ErrorMessage = "err.passwordRequired")]
    [DataType(DataType.Password)]
    [LStringLength(100, MinimumLength = 6, ErrorMessage = "err.passwordLength")]
    public string Password { get; set; } = string.Empty;

    // ทีม (เลือกจากรายการที่มีอยู่)
    [Display(Name = "field.team")]
    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    // ตำแหน่งหลักของพนักงาน (Role) ไม่บังคับ
    [Display(Name = "field.role")]
    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    // ความสัมพันธ์กับหัวหน้า (SubRole) ไม่บังคับ
    [Display(Name = "field.subRole")]
    public int? SubRoleId { get; set; }
    public SubRole? SubRole { get; set; }

    [Display(Name = "field.manager")]
    public int? ManagerId { get; set; }
    public Contact? Manager { get; set; }
    public ICollection<Contact> Subordinates { get; set; } = new List<Contact>();

    [Display(Name = "field.salary")]
    [LRange(0, 1000000000, ErrorMessage = "err.salaryRange")]
    public decimal? Salary { get; set; }
}