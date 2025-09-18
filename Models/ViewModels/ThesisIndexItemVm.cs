using ThesisNest.Models;

public class ThesisIndexItemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Department { get; set; } = default!;
    public ProposalStatus ProposalStatus { get; set; }
    public ThesisStatus? TeacherStatus { get; set; }
    public int CurrentVersionNo { get; set; }
    public string CreatedAtStr { get; set; } = default!;

    // Teacher info
    public int? TeacherId { get; set; }
    public string? TeacherFullName { get; set; }
}
