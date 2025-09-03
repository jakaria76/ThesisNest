namespace ThesisNest.Controllers
{
    internal class TeacherProfileVm
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public object Department { get; set; }
        public object Phone { get; set; }
        public string Office { get; set; }
        public string ProfileImageUrl { get; set; }
    }
}