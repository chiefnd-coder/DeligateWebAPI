namespace DeligateWebAPI.Models
{
    public class UserExistsResponse
    {
        public bool EmailExists { get; set; }
        public bool? PhoneNumberExists { get; set; }
    }
}
