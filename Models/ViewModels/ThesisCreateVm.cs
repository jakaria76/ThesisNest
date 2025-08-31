using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace ThesisNest.Models.ViewModels
{
    public class ThesisCreateVm
    {
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [Required, StringLength(4000, MinimumLength = 100)] public string Abstract { get; set; } = "";
        [Required] public int DepartmentId { get; set; }
        [Required] public int TeacherProfileId { get; set; }
        public string? Keywords { get; set; }


        [Required(ErrorMessage = "Please attach your proposal file (PDF/DOCX).")]
        public IFormFile File { get; set; } = null!;


        [StringLength(500)] public string? Note { get; set; }
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the declaration.")]
        public bool IsDeclared { get; set; }


        public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Supervisors { get; set; } = new List<SelectListItem>();
    }
}