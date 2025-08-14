using Microsoft.AspNetCore.Mvc;

namespace NotesApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Notes");
            }

            return Redirect("/Identity/Account/Login");
        }


    }
}
