namespace DeligateWebAPI.Models
{
    public class TokenResponse
    {
        public string DeviceToken { get; set; }
        public string Email { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? Suspension { get; set; }
        public bool? IsTokenActive { get; set; } 
    }
}
