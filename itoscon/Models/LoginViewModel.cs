using System.ComponentModel.DataAnnotations;

namespace itoscon.Models;

public class LoginViewModel
{
    [Display(Name = "อีเมล")]
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "รหัสผ่าน")]
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
