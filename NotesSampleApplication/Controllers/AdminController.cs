using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotesSampleApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotesApp.Controllers
{
    //[Authorize(Roles = "Admin")] // Only allow logged-in users to access this controller
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // View Model to pass more structured data to the view
        public class UserViewModel
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string CurrentRole { get; set; }
            public List<string> AllRoles { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();

            var usersWithRoles = new List<UserViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    CurrentRole = userRoles.FirstOrDefault() ?? "No Role",
                    AllRoles = roles
                });
            }

            return View(usersWithRoles);
        }

        [HttpPost] 
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            return RedirectToAction("Index");
        }
    }
}
