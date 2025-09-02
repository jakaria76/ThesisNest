using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.ViewModels
{
    public class ThesisCreateVm
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Abstract { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }
        public List<SelectListItem> Departments { get; set; } = new();

        [Required]
        public int TeacherProfileId { get; set; }
        public List<SelectListItem> Supervisors { get; set; } = new();

        public string Keywords { get; set; } = string.Empty;

        [Required]
        public IFormFile? File { get; set; }

        public string? Note { get; set; }

        [Required]
        public bool IsDeclared { get; set; }
    }
}
