namespace itoscon.Models;

// ข้อมูลที่ใช้แสดงในหน้ารายชื่อ (Select เฉพาะคอลัมน์ที่ใช้ ไม่ดึง Password)
public record ContactListItem(
    int Id,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    string? DivisionName,
    string? DepartmentName,
    string? TeamName,
    string? RoleName,
    string? ManagerName,
    string? SubRoleName,
    decimal? Salary)
{
    public string FullName => $"{FirstName} {LastName}";

    // ฝ่าย / แผนก / ทีม (แสดงเฉพาะส่วนที่มีค่า)
    public string? TeamPath => TeamName is null
        ? null
        : string.Join(" / ", new[] { DivisionName, DepartmentName, TeamName }.Where(x => !string.IsNullOrEmpty(x)));
}
