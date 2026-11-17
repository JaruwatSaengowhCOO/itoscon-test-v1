using System.ComponentModel.DataAnnotations;

namespace itoscon.Models;

public class Contact
{
    public int Id { get; set; }

    [Display(Name = "ชื่อ")]
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    [StringLength(100, ErrorMessage = "ชื่อยาวได้ไม่เกิน 100 ตัวอักษร")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "นามสกุล")]
    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    [StringLength(100, ErrorMessage = "นามสกุลยาวได้ไม่เกิน 100 ตัวอักษร")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "เบอร์โทร")]
    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [RegularExpression(@"^0\d{8,9}$", ErrorMessage = "เบอร์โทรต้องขึ้นต้นด้วย 0 และมี 9-10 หลัก (เช่น 0812345678)")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "อีเมล")]
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;
}
