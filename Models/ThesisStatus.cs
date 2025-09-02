namespace ThesisNest.Models
{
    public enum ThesisStatus
    {
        Proposed,    // Student uploaded, pending teacher review
        Pending,     // Waiting for teacher
        InProgress,  // Teacher is reviewing
        Completed,   // Teacher approved
        Rejected     // Teacher rejected
    }
}
