using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.modell
{
    public class UIModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [StringLength(50)]
        public string Role { get; set; }  // Admin, User, Guest

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public string ProfilePicture { get; set; } // path to uploaded image
    }
}
