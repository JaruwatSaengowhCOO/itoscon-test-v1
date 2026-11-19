using itoscon.Data;
using itoscon.Models;
using itoscon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Controllers;

[Authorize]
public class RolesController(AppDbContext db, IStaticText t) : Controller
{
    // GET: /Roles
    public async Task<IActionResult> Index()
    {
        var vm = new OrganizationViewModel
        {
            Roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
            SubRoles = await db.SubRoles.AsNoTracking().Include(s => s.Role)
                .OrderBy(s => s.Name).ToListAsync()
        };
        return View(vm);
    }

    // GET: /Roles/CreateRole
    public IActionResult CreateRole() => View(new Role());

    // POST: /Roles/CreateRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole([Bind("Name")] Role role)
    {
        if (!ModelState.IsValid) return View(role);

        role.Name = role.Name.Trim();
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        TempData["Message"] = string.Format(t["msg.roleAdded"], role.Name);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Roles/CreateSubRole
    public async Task<IActionResult> CreateSubRole()
    {
        await LoadRolesAsync();
        return View(new SubRole());
    }

    // POST: /Roles/CreateSubRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubRole([Bind("Name,RoleId")] SubRole subRole)
    {
        if (subRole.RoleId is int roleId && !await db.Roles.AnyAsync(r => r.Id == roleId))
            ModelState.AddModelError(nameof(SubRole.RoleId), t["err.roleNotFound"]);

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            return View(subRole);
        }

        subRole.Name = subRole.Name.Trim();
        db.SubRoles.Add(subRole);
        await db.SaveChangesAsync();
        TempData["Message"] = string.Format(t["msg.subRoleAdded"], subRole.Name);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRolesAsync()
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        ViewData["Roles"] = new SelectList(roles, "Id", "Name");
    }
}