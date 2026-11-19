using itoscon.Models;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<SubRole> SubRoles => Set<SubRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(e =>
        {
            e.Property(c => c.Salary).HasPrecision(18, 2);
            e.HasOne(c => c.Team).WithMany().HasForeignKey(c => c.TeamId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Role).WithMany().HasForeignKey(c => c.RoleId).OnDelete(DeleteBehavior.SetNull);
            // ห้ามลบความสัมพันธ์ที่ยังมีพนักงานใช้อยู่
            e.HasOne(c => c.SubRole).WithMany().HasForeignKey(c => c.SubRoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Manager).WithMany(c => c.Subordinates).HasForeignKey(c => c.ManagerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ลบฝ่าย/แผนกที่ยังมีแผนก/ทีมอยู่ไม่ได้
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Division).WithMany(d => d.Departments)
            .HasForeignKey(d => d.DivisionId).OnDelete(DeleteBehavior.Restrict);

        // ลบบทบาทแล้ว SubRole ที่ผูกอยู่จะกลับเป็นไม่ผูกบทบาท (null)
        modelBuilder.Entity<SubRole>()
            .HasOne(s => s.Role).WithMany(r => r.SubRoles)
            .HasForeignKey(s => s.RoleId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Team>()
            .HasOne(t => t.Department).WithMany(d => d.Teams)
            .HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
