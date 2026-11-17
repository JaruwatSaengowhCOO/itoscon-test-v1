using itoscon.Data;
using itoscon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace itoscon.Controllers;

public class ContactsController(AppDbContext db) : Controller
{
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

    // GET: /Contacts/Create
    public IActionResult Create() => View(new Contact());

    // POST: /Contacts/Create  — INSERT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Phone,Email")] Contact contact)
    {
        if (!ModelState.IsValid) return View(contact);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        TempData["Message"] = $"เพิ่มข้อมูล {contact.FirstName} {contact.LastName} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contacts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var contact = await db.Contacts.FindAsync(id);
        return contact is null ? NotFound() : View(contact);
    }

    // POST: /Contacts/Edit/5  — UPDATE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Phone,Email")] Contact input)
    {
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
