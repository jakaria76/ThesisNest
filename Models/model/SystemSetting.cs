using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.model
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Setting Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Setting Value")]
        public string Value { get; set; }
    }
}
