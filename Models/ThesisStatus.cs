namespace ThesisNest.Models
{
    public enum ThesisStatus
    {
        Pending,    // Student uploaded, pending teacher review
        Proposed,      // Waiting for teacher
        InProgress,  // Teacher is reviewing
        Completed,   // Teacher approved
        Rejected     // Teacher rejected
    }
}
