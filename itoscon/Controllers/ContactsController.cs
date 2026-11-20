using itoscon.Data;
using itoscon.Models;
using itoscon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using System.Security.Claims;

namespace itoscon.Controllers;

[Authorize]
public class ContactsController(AppDbContext db, IStaticText t) : Controller
{
    private const string CreateBind = "FirstName,LastName,Phone,Email,Password,TeamId,RoleId,SubRoleId,ManagerId,Salary";
    private const string EditBind = "Id,FirstName,LastName,Phone,Email,Password,TeamId,RoleId,SubRoleId,ManagerId,Salary";

    private readonly PasswordHasher<Contact> _hasher = new();

    // GET: /Contacts?search=&divisionId=&departmentId=&teamId=&roleId=&sort=&desc=&pageSize=&page=
    public async Task<IActionResult> Index([FromQuery] ContactListViewModel vm)
    {
        // รับเฉพาะค่าที่อนุญาต
        vm.Sort = vm.Sort is "name" or "lastName" or "team" or "salary" ? vm.Sort : "name";
        vm.PageSize = vm.PageSize is 10 or 25 or 50 ? vm.PageSize : 10;
        vm.Page = Math.Max(1, vm.Page);
        vm.Search = vm.Search?.Trim();

        // 1) ตัวกรอง: ทุกอย่างเป็น Where ที่ EF แปลเป็น SQL (ไม่ดึงข้อมูลมาคัดกรองใน C#)
        var query = db.Contacts.AsNoTracking().AsQueryable();

        // ค้นหาหลายคำ: แต่ละคำต้องตรงกับอย่างน้อยหนึ่งช่อง
        if (!string.IsNullOrWhiteSpace(vm.Search))
        {
            // รายการเงินเดือนที่ไม่ซ้ำกัน โหลดครั้งเดียวและโหลดเฉพาะเมื่อมีคำค้นที่เป็นตัวเลข
            List<decimal?>? salaryPool = null;

            foreach (var raw in vm.Search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                var term = raw; // คัดลอกค่าก่อนใช้ใน lambda

                // คำค้นเป็นตัวเลขหรือไม่ (ตัด , ออกก่อน เช่น 9,000 -> 9000)
                var digits = term.Replace(",", "");
                var isNumeric = digits.Any(char.IsDigit) && digits.All(ch => char.IsDigit(ch) || ch == '.');

                // เงินเดือนที่ตรงกับคำค้น (เก็บเป็นรายการว่างถ้าไม่ใช่ตัวเลข)
                var salaryHits = new List<decimal?>();
                if (isNumeric)
                {
                    salaryPool ??= await db.Contacts.AsNoTracking()
                        .Where(c => c.Salary != null)
                        .Select(c => c.Salary)
                        .Distinct()
                        .ToListAsync();

                    salaryHits = salaryPool
                        .Where(s => s!.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Contains(digits))
                        .ToList();
                }

                var hasSalaryHit = salaryHits.Count > 0; // ป้องกัน Contains ของรายการว่าง

                query = query.Where(c =>
                    c.FirstName.Contains(term) ||
                    c.LastName.Contains(term) ||
                    c.Phone.Contains(term) ||
                    c.Email.Contains(term) ||
                    (c.Team != null && (
                        c.Team.Name.Contains(term) ||
                        c.Team.Department.Name.Contains(term) ||
                        c.Team.Department.Division.Name.Contains(term))) ||
                    (c.Role != null && c.Role.Name.Contains(term)) ||
                    (c.SubRole != null && c.SubRole.Name.Contains(term)) ||
                    (hasSalaryHit && salaryHits.Contains(c.Salary)));
            }
        }

        if (vm.DivisionId is int divisionId)
            query = query.Where(c => c.Team != null && c.Team.Department.DivisionId == divisionId);

        if (vm.DepartmentId is int departmentId)
            query = query.Where(c => c.Team != null && c.Team.DepartmentId == departmentId);

        if (vm.TeamId is int teamId)
            query = query.Where(c => c.TeamId == teamId);

        if (vm.RoleId is int roleId)
            query = query.Where(c => c.RoleId == roleId);

        // 2) จำนวนทั้งหมด (ก่อนแบ่งหน้า)
        vm.TotalCount = await query.CountAsync();
        if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

        // 3) เรียงลำดับ: เลือกตามคอลัมน์และทิศทาง, ตัวสุดท้ายใช้ Id กันผลสลับกันเมื่อค่าเท่ากัน
        IOrderedQueryable<Contact> ordered = vm.Sort switch
        {
            "lastName" => vm.Desc
                ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),
            "team" => vm.Desc
                ? query.OrderByDescending(c => c.Team!.Name)
                : query.OrderBy(c => c.Team!.Name),
            "salary" => vm.Desc
                ? query.OrderByDescending(c => c.Salary)
                : query.OrderBy(c => c.Salary),
            _ => vm.Desc
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
        };
        ordered = ordered.ThenBy(c => c.Id);

        // 4) แบ่งหน้า + Select เฉพาะคอลัมน์ที่แสดง (ไม่มี Password)
        vm.Items = await ordered
            .Skip((vm.Page - 1) * vm.PageSize)
            .Take(vm.PageSize)
            .Select(c => new ContactListItem(
                c.Id,
                c.FirstName,
                c.LastName,
                c.Phone,
                c.Email,
                c.Team == null ? null : c.Team.Department.Division.Name,
                c.Team == null ? null : c.Team.Department.Name,
                c.Team == null ? null : c.Team.Name,
                c.Role == null ? null : c.Role.Name,
                c.Manager == null ? null : c.Manager.FirstName + " " + c.Manager.LastName,
                c.SubRole == null ? null : c.SubRole.Name,
                c.Salary))
            .ToListAsync();

        await LoadFilterListsAsync(vm);
        return View(vm);
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

    // รายการ dropdown ของตัวกรอง (ใส่ค่าที่เลือกไว้แล้ว)
    private async Task LoadFilterListsAsync(ContactListViewModel vm)
    {
        var divisions = await db.Divisions.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();
        vm.Divisions = divisions
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == vm.DivisionId))
            .ToList();

        var departments = await db.Departments.AsNoTracking()
            .OrderBy(d => d.Division.Name).ThenBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, Division = d.Division.Name })
            .ToListAsync();
        vm.Departments = departments
            .Select(d => new SelectListItem($"{d.Division} / {d.Name}", d.Id.ToString(), d.Id == vm.DepartmentId))
            .ToList();

        var teams = await db.Teams.AsNoTracking()
            .OrderBy(tm => tm.Department.Division.Name).ThenBy(tm => tm.Department.Name).ThenBy(tm => tm.Name)
            .Select(tm => new { tm.Id, tm.Name, Department = tm.Department.Name, Division = tm.Department.Division.Name })
            .ToListAsync();
        vm.Teams = teams
            .Select(tm => new SelectListItem($"{tm.Division} / {tm.Department} / {tm.Name}", tm.Id.ToString(), tm.Id == vm.TeamId))
            .ToList();

        var roles = await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();
        vm.Roles = roles
            .Select(r => new SelectListItem(r.Name, r.Id.ToString(), r.Id == vm.RoleId))
            .ToList();
    }
}
