using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;


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

    // GET: Notes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Notes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Note note) // <-- removed Bind
    {
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                System.Diagnostics.Debug.WriteLine($"Model Error: {error.ErrorMessage}");
            }
            return View(note);
        }

        var userId = _userManager.GetUserId(User);
        note.UserId = userId;
        note.CreatedAt = DateTime.Now;

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Note created successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Notes
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var notes = await _context.Notes
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notes);
    }

    // Intentional Vulnerability - SQL Injection 
    public async Task<IActionResult> Search(string searchTerm)
    {

        var userId = _userManager.GetUserId(User);

        var sql = $"SELECT * FROM Notes WHERE UserId = '{userId}' AND Title LIKE '%{searchTerm}%'";

        var notes = await _context.Notes.FromSqlRaw(sql).ToListAsync();

        return View("Index", notes);
    }


    // GET: Notes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId)
            .FirstOrDefaultAsync();

        if (note == null) return NotFound();

        return View(note);
    }

    // POST: Notes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Note note)
    {
        if (id != note.Id) return NotFound();

        var userId = _userManager.GetUserId(User);

        if (ModelState.IsValid)
        {
            try
            {
                // Get the existing note
                var existingNote = await _context.Notes
                    .Where(n => n.Id == id && n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (existingNote == null) return NotFound();

                // Update allowed fields
                existingNote.Title = note.Title;
                existingNote.Content = note.Content;

                _context.Update(existingNote);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Notes.Any(n => n.Id == note.Id && n.UserId == userId))
                    return NotFound();
                else
                    throw;
            }
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