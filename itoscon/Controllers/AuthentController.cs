using itoscon.Data;
using itoscon.Models;
using itoscon.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace itoscon.Controllers;

public class AuthentController(AppDbContext db, IStaticText t) : Controller
{
    private readonly PasswordHasher<Contact> _hasher = new();

    // GET: /Authent/Login
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Contacts");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Authent/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

        if (contact is null || string.IsNullOrEmpty(contact.Password) ||
            _hasher.VerifyHashedPassword(contact, contact.Password, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, t["login.invalid"]);
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, contact.Id.ToString()),
            new(ClaimTypes.Name, $"{contact.FirstName} {contact.LastName}"),
            new(ClaimTypes.Email, contact.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Contacts");
    }

    // POST: /Authent/Logout
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Message"] = t["msg.loggedOut"];
        return RedirectToAction(nameof(Login));
    }
}