namespace ThesisNest.Models.ViewModels
{
    public class TeacherViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Bio { get; set; }
        public int AvailableSlots { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
