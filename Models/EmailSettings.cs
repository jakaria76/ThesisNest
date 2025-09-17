namespace ThesisNest.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderName { get; set; } = "ThesisNest";
        public string SenderEmail { get; set; } = "";
        public string SmtpUsername { get; set; } = ""; // usually same as SenderEmail
        public string SmtpPassword { get; set; } = ""; // App password
        public bool UseSsl { get; set; } = true;
    }
}
