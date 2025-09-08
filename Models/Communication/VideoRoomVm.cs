namespace ThesisNest.Models;

public class VideoRoomVm
{
    public int ThreadId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public bool IsTeacher { get; set; }
}
