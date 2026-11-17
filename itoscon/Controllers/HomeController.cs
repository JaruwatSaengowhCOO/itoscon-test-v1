using itoscon.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace itoscon.Controllers
{
    public class HomeController : Controller
    {
        // ใช้เฉพาะหน้า Error (Program.cs: UseExceptionHandler("/Home/Error"))
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
