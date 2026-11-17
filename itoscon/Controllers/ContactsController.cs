using itoscon.Data;
using itoscon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Controllers;

[Authorize] // ทุก action ต้อง login ก่อน ยกเว้นที่ระบุ [AllowAnonymous]
public class ContactsController(AppDbContext db) : Controller
{
    private readonly PasswordHasher<Contact> _hasher = new();

    // GET: /Contacts  — ตารางแสดงข้อมูล (ค้นหาได้)
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Contacts.AsNoTracking();

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

    // GET: /Contacts/Create  — สร้าง contact ใหม่ (พร้อมรหัสผ่าน) ไม่ต้อง login
    [AllowAnonymous]
    public IActionResult Create()
    {
        ViewData["ShowPassword"] = true;
        return View(new Contact());
    }

    // POST: /Contacts/Create  — INSERT
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Phone,Email,Password")] Contact contact)
    {
        ViewData["ShowPassword"] = true;
        if (!ModelState.IsValid) return View(contact);

        contact.Email = contact.Email.Trim();
        if (await db.Contacts.AnyAsync(c => c.Email == contact.Email))
        {
            ModelState.AddModelError(nameof(Contact.Email), "อีเมลนี้ถูกใช้แล้ว");
            return View(contact);
        }

        contact.Password = _hasher.HashPassword(contact, contact.Password);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        TempData["Message"] = $"สร้างบัญชี {contact.FirstName} {contact.LastName} เรียบร้อยแล้ว กรุณาเข้าสู่ระบบ";
        return RedirectToAction("Login", "Account");
    }

    // GET: /Contacts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        return contact is null ? NotFound() : View(contact);
    }

    // POST: /Contacts/Edit/5  — UPDATE (ไม่แก้รหัสผ่านในหน้านี้)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Phone,Email")] Contact input)
    {
        ModelState.Remove(nameof(Contact.Password));
        if (id != input.Id) return BadRequest();
        if (!ModelState.IsValid) return View(input);

        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return NotFound();

        contact.FirstName = input.FirstName;
        contact.LastName = input.LastName;
        contact.Phone = input.Phone;
        contact.Email = input.Email;
        await db.SaveChangesAsync();

        TempData["Message"] = $"แก้ไขข้อมูล {contact.FirstName} {contact.LastName} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contacts/Delete/5  — หน้ายืนยันการลบ
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return contact is null ? NotFound() : View(contact);
    }

    // POST: /Contacts/Delete/5  — DELETE
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        if (contact is not null)
        {
            db.Contacts.Remove(contact);
            await db.SaveChangesAsync();
            TempData["Message"] = $"ลบข้อมูล {contact.FirstName} {contact.LastName} เรียบร้อยแล้ว";
        }
        return RedirectToAction(nameof(Index));
    }
}
