namespace DeligateWebAPI.Models
{
    public class CreateVendorRatingDto
    {
        public string VendorEmail { get; set; }
        public string VendorName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
