namespace DeligateWebAPI.Models
{
    public class VendorRegistrationRequest
    {

        public string FullNames { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Website { get; set; }
        public string Country { get; set; }
        public string CompanyName { get; set; }
        public string CompanyRegistrationNumber { get; set; }
        public string AreaOfSpecialization { get; set; }
        public string AreaOfService { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string? Linkedin { get; set; }
        public string? Tiktok { get; set; }
        public string Instagram { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? TiktokUrl { get; set; }
        public string IdNumber { get; set; }
        public string UploadedIdPath { get; set; }
        public string UploadedPhotoPath { get; set; }
        public string DeviceToken { get; set; }
    }
}
