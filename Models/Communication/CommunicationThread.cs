using System;
using System.Collections.Generic;

namespace ThesisNest.Models;

public class CommunicationThread
{
    public int Id { get; set; }
    public int TeacherProfileId { get; set; }
    public int StudentProfileId { get; set; }
    public int? ThesisId { get; set; }
    public bool IsEnabled { get; set; } = false; // Thesis Accept হলে true
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TeacherProfile Teacher { get; set; } = default!;
    public StudentProfile Student { get; set; } = default!;

    public ICollection<CallSession> Calls { get; set; } = new List<CallSession>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

}
