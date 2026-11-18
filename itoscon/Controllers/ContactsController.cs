using itoscon.Data;
using itoscon.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
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

    // GET: /Contacts/Create  — เพิ่มผู้ใช้ใหม่ 
    public IActionResult Create()
    {
        ViewData["ShowPassword"] = true;
        return View(new Contact());
    }

    // POST: /Contacts/Create  — INSERT 
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

        TempData["Message"] = $"เพิ่มผู้ใช้ {contact.FirstName} {contact.LastName} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contacts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return NotFound();

        contact.Password = string.Empty; // ไม่ส่ง hash ออกไปที่หน้าเว็บ
        ViewData["ShowPassword"] = true;
        return View(contact);
    }

    // POST: /Contacts/Edit/5  — UPDATE (เปลี่ยนรหัสผ่านได้ถ้ากรอกช่องรหัสผ่านใหม่ ถ้าเว้นว่างคือไม่เปลี่ยน)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Phone,Email,Password")] Contact input)
    {
        ViewData["ShowPassword"] = true;
        if (id != input.Id) return BadRequest();

        // รหัสผ่านเป็นทางเลือกในหน้านี้ จึงไม่ใช้ Required ของ model
        ModelState.Remove(nameof(Contact.Password));
        var newPassword = input.Password;
        if (!string.IsNullOrEmpty(newPassword) && (newPassword.Length < 6 || newPassword.Length > 100))
            ModelState.AddModelError(nameof(Contact.Password), "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร");

        if (!ModelState.IsValid) return View(input);

        var contact = await db.Contacts.FindAsync(id);
        if (contact is null) return NotFound();

        input.Email = input.Email.Trim();
        if (await db.Contacts.AnyAsync(c => c.Email == input.Email && c.Id != id))
        {
            ModelState.AddModelError(nameof(Contact.Email), "อีเมลนี้ถูกใช้แล้ว");
            return View(input);
        }

        contact.FirstName = input.FirstName;
        contact.LastName = input.LastName;
        contact.Phone = input.Phone;
        contact.Email = input.Email;
        if (!string.IsNullOrEmpty(newPassword))
            contact.Password = _hasher.HashPassword(contact, newPassword);
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
        // ห้ามลบบัญชีที่ login อยู่ (ป้องกันการลบตัวเอง)
        if (id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = "ไม่สามารถลบบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(Index));
        }

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
