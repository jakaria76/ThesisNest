using ThesisNest.Models;

public class ThesisIndexItemVm
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Department { get; set; }
    public ProposalStatus ProposalStatus { get; set; }
    public ThesisStatus? TeacherStatus { get; set; }
    public int CurrentVersionNo { get; set; }
    public string CreatedAtStr { get; set; }

    // Teacher info
    public int? TeacherId { get; set; }
    public string? TeacherFullName { get; set; }
}
