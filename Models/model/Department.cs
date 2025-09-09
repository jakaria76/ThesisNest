using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.model
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
