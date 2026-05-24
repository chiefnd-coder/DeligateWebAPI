namespace DeligateWebAPI.Models
{
    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string _CountryCode { get; set; }
        public string Password { get; set; }
        public string DeviceToken { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
