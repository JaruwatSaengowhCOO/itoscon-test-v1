using itoscon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace itoscon.Data;

public static class DbInitializer
{
    // ปรับโครงสร้างฐานข้อมูลให้ตรงกับโมเดลปัจจุบัน (รองรับทั้งฐานข้อมูลใหม่และเดิม)
    public static void EnsureSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) conn.Open();
        try
        {
            // 1) เปลี่ยนชื่อจาก Position เดิมเป็น Role (ทำก่อนสร้างตาราง เพื่อไม่ให้ข้อมูลหาย)
            RenameTableIfExists(conn, "Positions", "Roles");
            RenameColumnIfExists(conn, "Contacts", "PositionId", "RoleId");

            // 2) ตารางที่ยังไม่มี (ตารางที่ถูกอ้างอิงต้องสร้างก่อน)
            Exec(conn, "CREATE TABLE IF NOT EXISTS Divisions (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)");
            Exec(conn, "CREATE TABLE IF NOT EXISTS Roles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)");
            Exec(conn, @"CREATE TABLE IF NOT EXISTS SubRoles (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            RoleId INTEGER NULL REFERENCES Roles(Id) ON DELETE SET NULL)");
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Departments (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            DivisionId INTEGER NOT NULL REFERENCES Divisions(Id) ON DELETE RESTRICT)");
            Exec(conn, @"CREATE TABLE IF NOT EXISTS Teams (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            DepartmentId INTEGER NOT NULL REFERENCES Departments(Id) ON DELETE RESTRICT)");

            // 3) คอลัมน์ใหม่ในตาราง Contacts
            // SubRoleId และ RoleId ของ SubRole เป็น NULL ได้ (ความสัมพันธ์ไม่บังคับ)
            AddColumnIfMissing(conn, "Contacts", "Password", "TEXT NOT NULL DEFAULT ''");
            AddColumnIfMissing(conn, "Contacts", "TeamId", "INTEGER NULL REFERENCES Teams(Id) ON DELETE SET NULL");
            AddColumnIfMissing(conn, "Contacts", "RoleId", "INTEGER NULL REFERENCES Roles(Id) ON DELETE SET NULL");
            AddColumnIfMissing(conn, "Contacts", "SubRoleId", "INTEGER NULL REFERENCES SubRoles(Id) ON DELETE RESTRICT");
            AddColumnIfMissing(conn, "Contacts", "ManagerId", "INTEGER NULL REFERENCES Contacts(Id) ON DELETE SET NULL");
            AddColumnIfMissing(conn, "Contacts", "Salary", "NUMERIC(18,2) NULL");
        }
        finally
        {
            if (wasClosed) conn.Close();
        }
    }

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

    private static void RenameTableIfExists(IDbConnection conn, string from, string to)
    {
        if (TableExists(conn, from) && !TableExists(conn, to))
            Exec(conn, $"ALTER TABLE {from} RENAME TO {to}");
    }

    private static void RenameColumnIfExists(IDbConnection conn, string table, string from, string to)
    {
        if (ColumnExists(conn, table, from) && !ColumnExists(conn, table, to))
            Exec(conn, $"ALTER TABLE {table} RENAME COLUMN {from} TO {to}");
    }

    private static void AddColumnIfMissing(IDbConnection conn, string table, string column, string definition)
    {
        if (!ColumnExists(conn, table, column))
            Exec(conn, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }

    private static bool TableExists(IDbConnection conn, string table) =>
        Scalar(conn, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{table}'") > 0;

    private static bool ColumnExists(IDbConnection conn, string table, string column) =>
        Scalar(conn, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'") > 0;

    private static long Scalar(IDbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static void Exec(IDbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
