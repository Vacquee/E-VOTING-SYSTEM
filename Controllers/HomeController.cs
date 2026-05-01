using Microsoft.AspNetCore.Mvc;

namespace VotingSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var isAdmin = User.IsInRole("Admin");
                if (isAdmin)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Dashboard", "Student");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
