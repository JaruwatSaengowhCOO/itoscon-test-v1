using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace itoscon.Controllers
{
    public class LanguageController : Controller
    {
        private static readonly string[] Supported = { "th-TH", "en-US" };

        // POST: /Language/Set
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Set(string culture, string? returnUrl)
        {
            if (!Supported.Contains(culture)) culture = "th-TH";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            // LocalRedirect รับเฉพาะ URL ภายในเว็บ ป้องกัน open redirect
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
    }
}
