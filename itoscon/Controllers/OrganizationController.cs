using itoscon.Services;
using itoscon.Data;
using itoscon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Controllers;

[Authorize]
public class OrganizationController(AppDbContext db, IStaticText t) : Controller
{
    // GET: /Organization
    public async Task<IActionResult> Index()
    {
        var vm = new OrganizationViewModel
        {
            Divisions = await db.Divisions.AsNoTracking()
                .Include(d => d.Departments).ThenInclude(dep => dep.Teams)
                .OrderBy(d => d.Name).ToListAsync(),
            Departments = await db.Departments.AsNoTracking()
                .Include(d => d.Division)
                .OrderBy(d => d.Division.Name).ThenBy(d => d.Name).ToListAsync(),
            Roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
            SubRoles = await db.SubRoles.AsNoTracking().Include(s => s.Role)
                .OrderBy(s => s.Name).ToListAsync()
        };
        return View(vm);
    }

    // ---------- เพิ่ม ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDivision(string name)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.divisionName"]);

        db.Divisions.Add(new Division { Name = clean });
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.divisionAdded"], clean));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string name, int divisionId)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.departmentName"]);
        if (!await db.Divisions.AnyAsync(d => d.Id == divisionId)) return Fail(t["err.divisionNotFound"]);

        db.Departments.Add(new Department { Name = clean, DivisionId = divisionId });
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.departmentAdded"], clean));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeam(string name, int departmentId)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.teamName"]);
        if (!await db.Departments.AnyAsync(d => d.Id == departmentId)) return Fail(t["err.departmentNotFound"]);

        db.Teams.Add(new Team { Name = clean, DepartmentId = departmentId });
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.teamAdded"], clean));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(string name)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.roleName"]);

        db.Roles.Add(new Role { Name = clean });
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.roleAdded"], clean));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubRole(string name, int? roleId)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.subRoleName"]);
        if (roleId is int rid && !await db.Roles.AnyAsync(r => r.Id == rid)) return Fail(t["err.roleNotFound"]);

        db.SubRoles.Add(new SubRole { Name = clean, RoleId = roleId });
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.subRoleAdded"], clean));
    }

    // ---------- แก้ไขชื่อ ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameDivision(int id, string name)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.divisionName"]);
        var item = await db.Divisions.FindAsync(id);
        if (item is null) return Fail(t["err.divisionNotFound"]);

        item.Name = clean;
        await db.SaveChangesAsync();
        return Done(t["msg.divisionRenamed"]);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameDepartment(int id, string name)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.departmentName"]);
        var item = await db.Departments.FindAsync(id);
        if (item is null) return Fail(t["err.departmentNotFound"]);

        item.Name = clean;
        await db.SaveChangesAsync();
        return Done(t["msg.departmentRenamed"]);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameTeam(int id, string name)
    {
        var clean = CleanName(name);
        if (clean is null) return Fail(t["err.teamName"]);
        var item = await db.Teams.FindAsync(id);
        if (item is null) return Fail(t["err.teamNotFound"]);

        item.Name = clean;
        await db.SaveChangesAsync();
        return Done(t["msg.teamRenamed"]);
    }

    // ---------- ลบ (ลบได้เฉพาะเมื่อไม่มีรายการลูกหรือพนักงานใช้อยู่) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDivision(int id)
    {
        var item = await db.Divisions.FindAsync(id);
        if (item is null) return Fail(t["err.divisionNotFound"]);
        if (await db.Departments.AnyAsync(d => d.DivisionId == id))
            return Fail(string.Format(t["err.divisionHasDepartments"], item.Name));

        db.Divisions.Remove(item);
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.divisionDeleted"], item.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var item = await db.Departments.FindAsync(id);
        if (item is null) return Fail(t["err.departmentNotFound"]);
        if (await db.Teams.AnyAsync(tm => tm.DepartmentId == id))
            return Fail(string.Format(t["err.departmentHasTeams"], item.Name));

        db.Departments.Remove(item);
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.departmentDeleted"], item.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var item = await db.Teams.FindAsync(id);
        if (item is null) return Fail(t["err.teamNotFound"]);
        if (await db.Contacts.AnyAsync(c => c.TeamId == id))
            return Fail(string.Format(t["err.teamHasContacts"], item.Name));

        db.Teams.Remove(item);
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.teamDeleted"], item.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var item = await db.Roles.FindAsync(id);
        if (item is null) return Fail(t["err.roleNotFound"]);
        if (await db.Contacts.AnyAsync(c => c.RoleId == id))
            return Fail(string.Format(t["err.roleInUse"], item.Name));

        // ความสัมพันธ์ที่ผูกกับตำแหน่งนี้จะกลับเป็นไม่ผูก (SetNull)
        db.Roles.Remove(item);
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.roleDeleted"], item.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubRole(int id)
    {
        var item = await db.SubRoles.FindAsync(id);
        if (item is null) return Fail(t["err.subRoleNotFound"]);
        if (await db.Contacts.AnyAsync(c => c.SubRoleId == id))
            return Fail(string.Format(t["err.subRoleInUse"], item.Name));

        db.SubRoles.Remove(item);
        await db.SaveChangesAsync();
        return Done(string.Format(t["msg.subRoleDeleted"], item.Name));
    }

    // ---------- helpers ----------

    private static string? CleanName(string? value, int max = 100)
    {
        var name = (value ?? string.Empty).Trim();
        return name.Length == 0 || name.Length > max ? null : name;
    }

    private IActionResult Done(string message)
    {
        TempData["Message"] = message;
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Fail(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}