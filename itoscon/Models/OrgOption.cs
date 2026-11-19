namespace itoscon.Models;

// รายการฝ่าย/แผนก/ทีม สำหรับ dropdown แบบลำดับชั้น (ParentId = รหัสของชั้นบนที่ผูกอยู่)
public record OrgOption(int Id, string Name, int ParentId);