using itoscon.Data;
using itoscon.Models;
using itoscon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace itoscon.Controllers;

[Authorize]
public class ContactsController(AppDbContext db, IStaticText t) : Controller
{
    private const string CreateBind = "FirstName,LastName,Phone,Email,Password,TeamId,RoleId,SubRoleId,ManagerId,Salary";
    private const string EditBind = "Id,FirstName,LastName,Phone,Email,Password,TeamId,RoleId,SubRoleId,ManagerId,Salary";

    private readonly PasswordHasher<Contact> _hasher = new();

    // GET: /Contacts
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Contacts.AsNoTracking()
            .Include(c => c.Team)
            .Include(c => c.Role)
            .Include(c => c.SubRole)
            .Include(c => c.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                c.Phone.Contains(search) ||
                c.Email.Contains(search));
        }

        ViewData["Search"] = search;
        return View(await query.OrderBy(c => c.Id).ToListAsync());
    }

    // GET: /Contacts/Create
    public async Task<IActionResult> Create()
    {
        ViewData["ShowPassword"] = true;
        await LoadLookupsAsync(null, null);
        return View(new Contact());
    }

    // POST: /Contacts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(CreateBind)] Contact contact)
    {
        ViewData["ShowPassword"] = true;
        contact.Email = (contact.Email ?? string.Empty).Trim();

        if (ModelState.IsValid && await db.Contacts.AnyAsync(c => c.Email == contact.Email))
            ModelState.AddModelError(nameof(Contact.Email), t["err.emailDuplicate"]);

        if (ModelState.IsValid)
            await ValidateReferencesAsync(contact, null);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(null, contact.TeamId);
            return View(contact);
        }

        contact.Password = _hasher.HashPassword(contact, contact.Password);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        TempData["Message"] = string.Format(t["msg.contactAdded"], $"{contact.FirstName} {contact.LastName}");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contacts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return NotFound();

        contact.Password = string.Empty; // ไม่ส่ง hash ออกไปที่หน้าเว็บ
        ViewData["ShowPassword"] = true;
        ViewData["PasswordOptional"] = true;
        await LoadLookupsAsync(id, contact.TeamId);
        return View(contact);
    }

    // POST: /Contacts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(EditBind)] Contact input)
    {
        ViewData["ShowPassword"] = true;
        ViewData["PasswordOptional"] = true;
        if (id != input.Id) return BadRequest();

        input.Email = (input.Email ?? string.Empty).Trim();

        // รหัสผ่านเป็นทางเลือกในหน้านี้ ยกเลิก Required แล้วตรวจความยาวเองเมื่อมีการกรอก
        ModelState.Remove(nameof(Contact.Password));
        if (!string.IsNullOrEmpty(input.Password) && (input.Password.Length < 6 || input.Password.Length > 100))
            ModelState.AddModelError(nameof(Contact.Password), t["err.passwordLength"]);

        if (ModelState.IsValid && await db.Contacts.AnyAsync(c => c.Email == input.Email && c.Id != id))
            ModelState.AddModelError(nameof(Contact.Email), t["err.emailDuplicate"]);

        if (ModelState.IsValid)
            await ValidateReferencesAsync(input, id);

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(id, input.TeamId);
            return View(input);
        }

        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return NotFound();

        contact.FirstName = input.FirstName;
        contact.LastName = input.LastName;
        contact.Phone = input.Phone;
        contact.Email = input.Email;
        contact.TeamId = input.TeamId;
        contact.SubRoleId = input.SubRoleId;
        contact.RoleId = input.RoleId;
        contact.ManagerId = input.ManagerId;
        contact.Salary = input.Salary;
        if (!string.IsNullOrEmpty(input.Password))
            contact.Password = _hasher.HashPassword(contact, input.Password);

        await db.SaveChangesAsync();

        TempData["Message"] = string.Format(t["msg.contactUpdated"], $"{contact.FirstName} {contact.LastName}");
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contacts/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return contact is null ? NotFound() : View(contact);
    }

    // POST: /Contacts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = t["err.cannotDeleteSelf"];
            return RedirectToAction(nameof(Index));
        }

        var contact = await db.Contacts.FindAsync(id);
        if (contact is not null)
        {
            // ลูกน้องที่มีหัวหน้าคนนี้จะถูกตั้งหัวหน้าเป็นว่าง (ClientSetNull: ต้องโหลดลูกน้องให้ EF ติดตามก่อนลบ)
            await db.Contacts.Where(c => c.ManagerId == id).LoadAsync();
            db.Contacts.Remove(contact);
            await db.SaveChangesAsync();
            TempData["Message"] = string.Format(t["msg.contactDeleted"], $"{contact.FirstName} {contact.LastName}");
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    // ตรวจว่าทีม/ตำแหน่ง/ความสัมพันธ์/หัวหน้ามีอยู่จริง และหัวหน้าไม่วนเป็นลูปกลับมาหาตัวเอง
    private async Task ValidateReferencesAsync(Contact input, int? contactId)
    {
        if (input.TeamId is int teamId && !await db.Teams.AnyAsync(tm => tm.Id == teamId))
            ModelState.AddModelError(nameof(Contact.TeamId), t["err.teamNotFound"]);

        if (input.RoleId is int roleId && !await db.Roles.AnyAsync(r => r.Id == roleId))
            ModelState.AddModelError(nameof(Contact.RoleId), t["err.roleNotFound"]);

        if (input.SubRoleId is int subRoleId && !await db.SubRoles.AnyAsync(s => s.Id == subRoleId))
            ModelState.AddModelError(nameof(Contact.SubRoleId), t["err.subRoleNotFound"]);

        if (input.ManagerId is int managerId)
        {
            if (contactId is int self && managerId == self)
            {
                ModelState.AddModelError(nameof(Contact.ManagerId), t["err.managerSelf"]);
                return;
            }

            if (!await db.Contacts.AnyAsync(c => c.Id == managerId))
            {
                ModelState.AddModelError(nameof(Contact.ManagerId), t["err.managerNotFound"]);
                return;
            }

            // เดินขึ้นไปตามสายหัวหน้า ถ้าเจอตัวเองแปลว่าจะเกิดวงวน
            if (contactId is int editing)
            {
                int? cursor = managerId;
                for (var depth = 0; cursor is int current && depth < 1000; depth++)
                {
                    if (current == editing)
                    {
                        ModelState.AddModelError(nameof(Contact.ManagerId), t["err.managerLoop"]);
                        return;
                    }
                    cursor = await db.Contacts.AsNoTracking()
                        .Where(c => c.Id == current)
                        .Select(c => c.ManagerId)
                        .FirstOrDefaultAsync();
                }
            }
        }
    }

    // โหลดรายการ dropdown ทั้งหมด และตั้งค่าฝ่าย/แผนกที่ควรเลือกจากทีมที่เคยบันทึกไว้
    private async Task LoadLookupsAsync(int? excludeContactId, int? selectedTeamId)
    {
        var divisions = await db.Divisions.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();
        ViewData["Divisions"] = new SelectList(divisions, "Id", "Name");

        ViewData["Departments"] = await db.Departments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new OrgOption(d.Id, d.Name, d.DivisionId))
            .ToListAsync();

        ViewData["Teams"] = await db.Teams.AsNoTracking()
            .OrderBy(tm => tm.Name)
            .Select(tm => new OrgOption(tm.Id, tm.Name, tm.DepartmentId))
            .ToListAsync();

        // หาฝ่ายและแผนกของทีมที่เลือกไว้ เพื่อให้ dropdown แสดงค่าเดิมตอนแก้ไข
        ViewData["SelDivisionId"] = null;
        ViewData["SelDepartmentId"] = null;
        if (selectedTeamId is int tid)
        {
            var team = await db.Teams.AsNoTracking()
                .Include(tm => tm.Department)
                .FirstOrDefaultAsync(tm => tm.Id == tid);
            if (team is not null)
            {
                ViewData["SelDivisionId"] = team.Department.DivisionId;
                ViewData["SelDepartmentId"] = team.DepartmentId;
            }
        }

        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        ViewData["Roles"] = new SelectList(roles, "Id", "Name");

        var subRoles = await db.SubRoles.AsNoTracking()
            .Include(s => s.Role)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, Name = s.Role == null ? s.Name : s.Name + " (" + s.Role.Name + ")" })
            .ToListAsync();
        ViewData["SubRoles"] = new SelectList(subRoles, "Id", "Name");

        var managers = await db.Contacts.AsNoTracking()
            .Where(c => excludeContactId == null || c.Id != excludeContactId)
            .OrderBy(c => c.FirstName)
            .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName })
            .ToListAsync();
        ViewData["Managers"] = new SelectList(managers, "Id", "Name");
    }
}