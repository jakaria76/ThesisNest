namespace ThesisNest.Models
{
    // Proposal-level status (separate from Thesis lifecycle)
    public enum ProposalStatus
    {
        Draft = 0,
        Submitted = 1,
        UnderReview = 2,
        ChangesRequested = 3,
        Approved = 4,
        Rejected = 5
    }
}