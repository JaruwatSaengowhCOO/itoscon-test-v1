using itoscon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace itoscon.Data;

public static class DbInitializer
{
    // เพิ่มคอลัมน์ Password ให้ตาราง Contacts ถ้ายังไม่มี (รองรับฐานข้อมูลเดิม)
    public static void EnsureSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) conn.Open();
        try
        {
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Contacts') WHERE name = 'Password'";
                if (Convert.ToInt64(check.ExecuteScalar()) == 0)
                {
                    using var alter = conn.CreateCommand();
                    alter.CommandText = "ALTER TABLE Contacts ADD COLUMN Password TEXT NOT NULL DEFAULT ''";
                    alter.ExecuteNonQuery();
                }
            }
        }
        finally
        {
            if (wasClosed) conn.Close();
        }
    }

    // สร้างผู้ใช้ภายในคนแรก เฉพาะตอนที่ตารางว่าง และตั้งค่า InitialAdmin ไว้ (ผ่าน user-secrets หรือ environment variable)
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
