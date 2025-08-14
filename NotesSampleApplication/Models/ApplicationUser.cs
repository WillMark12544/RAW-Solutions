using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace NotesSampleApplication.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Note> Notes { get; set; }
    }
}
