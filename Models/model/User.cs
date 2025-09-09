using System.ComponentModel.DataAnnotations;
namespace ThesisNest.Models.model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Store hashed password

        [Required]
        public string Role { get; set; } // e.g., Student / Supervisor / Admin

        public bool IsApproved { get; set; }  // Supervisor approval

        public string ContactNumber { get; set; }

    }
}
