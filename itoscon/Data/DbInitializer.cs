using itoscon.Models;
using Microsoft.AspNetCore.Identity;

namespace itoscon.Data;

// โครงสร้างตารางจัดการด้วย EF Core Migrations (โฟลเดอร์ Migrations) — คลาสนี้ทำแค่ seed ข้อมูลเริ่มต้น
public static class DbInitializer
{
    // สร้างผู้ใช้ภายในคนแรก เฉพาะตอนที่ตารางว่าง และตั้งค่า InitialAdmin ไว้
    public static void SeedInitialAdmin(AppDbContext db, IConfiguration config)
    {
        var email = config["InitialAdmin:Email"];
        var password = config["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (db.Contacts.Any()) return;

        var admin = new Contact
        {
            FirstName = "Admin",
            LastName = "System",
            Phone = "0000000000",
            Email = email.Trim()
        };
        admin.Password = new PasswordHasher<Contact>().HashPassword(admin, password);
        db.Contacts.Add(admin);
        db.SaveChanges();
    }
}
