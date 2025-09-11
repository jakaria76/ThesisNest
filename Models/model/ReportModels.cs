namespace ThesisNest.Models.model
{
    public class DepartmentReport
    {
        public string DepartmentName { get; set; }
        public int ThesisCount { get; set; }
    }

    public class SupervisorReport
    {
        public string SupervisorName { get; set; }
        public int ThesisReviewed { get; set; }
    }
}
