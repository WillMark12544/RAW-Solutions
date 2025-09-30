using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;
using Xunit;

namespace NotesSampleApplication.Tests.Unit
{
    public class NotesControllerUnitTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly NotesController _controller;
        private readonly string _testUserId = "test-user-123";

        public NotesControllerUnitTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            _controller = new NotesController(_context, _userManagerMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Name, "testuser@example.com")
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        }


        [Fact] // TEst 1: CREATE - Tests Note is added to database. The note has correct details
        public async Task Create_POST_ValidNote_SavesNoteToDatabase()
        {

            //Test Data
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            var newNote = new Note
            {
                Title = "Test Note Title",
                Content = "Test Note Content"
            };

            _controller.ModelState.Clear();

            var countBefore = await _context.Notes.CountAsync();

            var result = await _controller.Create(newNote);

            // Go to index page
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);


            //should be 1 more note in database
            var countAfter = await _context.Notes.CountAsync();
            Assert.Equal(countBefore + 1, countAfter);


            // Saaved note has correct details
            var savedNote = await _context.Notes.FirstOrDefaultAsync(n => n.Title == "Test Note Title");
            Assert.NotNull(savedNote);
            Assert.Equal("Test Note Content", savedNote.Content);
            Assert.Equal(_testUserId, savedNote.UserId);
        }

        [Fact] // TEST 2: eDIT - Edited note is updated in database
        public async Task Edit_POST_UpdatesNoteInDatabase()
        {
            // Test data
            var existingNote = new Note
            {
                Id = 50,
                Title = "Original Title",
                Content = "Original Content",
                UserId = _testUserId,
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            _context.Notes.Add(existingNote);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(_testUserId);

            // updated note
            var updatedNote = new Note
            {
                Id = 50,
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _controller.ModelState.Clear();

            var result = await _controller.Edit(50, updatedNote);

            
            // Go index page
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Checks database was updated
            var noteInDb = await _context.Notes.FindAsync(50);
            Assert.NotNull(noteInDb);
            Assert.Equal("Updated Title", noteInDb.Title);
            Assert.Equal("Updated Content", noteInDb.Content);
            Assert.Equal(_testUserId, noteInDb.UserId); 
        }

        [Fact] // TEST 3: DElete - Checks note was deleted from database
        public async Task Delete_POST_RemovesNoteFromDatabase()
        {

            // Test data to delete
            var noteToDelete = new Note
            {
                Id = 75,
                Title = "Note to Delete",
                Content = "This will be deleted",
                UserId = _testUserId,
                CreatedAt = DateTime.Now
            };

            _context.Notes.Add(noteToDelete);
            await _context.SaveChangesAsync();

            var testUser = new ApplicationUser { Id = _testUserId };
            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            var countBefore = await _context.Notes.CountAsync();

            // Calls delete methods
            var result = await _controller.DeleteConfirmed(75);


            // Go index
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // checks 1 less note (simple way of checking)
            var countAfter = await _context.Notes.CountAsync();
            Assert.Equal(countBefore - 1, countAfter);

            var deletedNote = await _context.Notes.FindAsync(75);
            Assert.Null(deletedNote); 
        }

        [Fact] //TEST 4: AUTHORIZE - tests only the current users notes are shown. Others notes arn't shown 
        public async Task Index_GET_ReturnsOnlyCurrentUserNotes()
        {

            // 2 notes from diff user IDs
            var currentUserNote = new Note
            {
                Title = "My Note",
                Content = "My Content",
                UserId = _testUserId,
                CreatedAt = DateTime.Now
            };

            var otherUserNote = new Note
            {
                Title = "Someone Else's Note",
                Content = "Secret Content",
                UserId = "different-user-999",
                CreatedAt = DateTime.Now
            };

            _context.Notes.AddRange(currentUserNote, otherUserNote);
            await _context.SaveChangesAsync();

            var testUser = new ApplicationUser { Id = _testUserId };
            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Note>>(viewResult.Model);

            Assert.Single(model);
            Assert.Equal("My Note", model[0].Title);
            Assert.Equal(_testUserId, model[0].UserId);
            Assert.DoesNotContain(model, n => n.Title == "Someone Else's Note");
        }

        // Cleanup
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}