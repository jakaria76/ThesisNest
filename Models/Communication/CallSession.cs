using System;

namespace ThesisNest.Models;

public class CallSession
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public CommunicationThread Thread { get; set; } = default!;

    public CommunicationType Type { get; set; } // Audio or Video
    public string StartedByUserId { get; set; } = default!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}
