using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotesSampleApplication.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Link note to user
        public string? UserId { get; set; }   // <-- make nullable

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }  // <-- make nullable
    }

}
