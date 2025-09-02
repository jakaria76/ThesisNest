using System.Collections.Generic;

namespace ThesisNest.Models.ViewModels
{
    public class ThesisDetailsVm
    {
        public Thesis Thesis { get; set; } = null!;
        public List<ThesisVersion> Versions { get; set; } = new();
        public List<ThesisFeedback> Feedbacks { get; set; } = new();
    }
}
