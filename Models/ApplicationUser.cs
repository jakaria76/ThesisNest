using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public string? Batch { get; set; }
    public string? StudentId { get; set; }
    //public string Role { get; internal set; }
    public bool IsApproved { get; internal set; }
}
