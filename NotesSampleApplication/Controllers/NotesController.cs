using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NotesSampleApplication.Controllers
{
    [Authorize]
    public class NotesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Notes
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var notes = await _context.Notes
                .Where(n => n.UserId == user.Id)
                .ToListAsync();
            return View(notes);
        }

        // GET: Notes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Notes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Note note)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                note.UserId = user.Id;
                note.CreatedAt = DateTime.Now;

                _context.Add(note);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index)); // back to Notes homepage
            }
            return View(note);
        }

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var note = await _context.Notes.FindAsync(id);
            if (note == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (note.UserId != user.Id) return Unauthorized();

            return View(note);
        }

        // POST: Notes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Note note)
        {
            if (id != note.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (note.UserId != user.Id) return Unauthorized();

            if (ModelState.IsValid)
            {
                _context.Update(note);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(note);
        }

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var note = await _context.Notes.FindAsync(id);
            if (note == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (note.UserId != user.Id) return Unauthorized();

            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (note.UserId != user.Id) return Unauthorized();

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
