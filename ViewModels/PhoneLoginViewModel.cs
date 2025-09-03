namespace ThesisNest.ViewModels
{
    public class PhoneLoginViewModel
    {
        public string? PhoneNumber { get; set; }   // User's phone number
        public string? Password { get; set; }      // Optional for OTP-less login
        public bool RememberMe { get; set; }      // Keep the user logged in
    }
}
