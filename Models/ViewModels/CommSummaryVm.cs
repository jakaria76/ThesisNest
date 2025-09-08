namespace ThesisNest.Models.ViewModels
{
    public class CommSummaryVm
    {
        public int ActiveThreads { get; set; }
        public int SmsStudents { get; set; }
        public int AudioStudents { get; set; }
        public int VideoStudents { get; set; }

        public List<(int StudentProfileId, string StudentName)> Threads { get; set; } = new();
    }
}
