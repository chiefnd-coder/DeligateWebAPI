namespace DeligateWebAPI.Models
{
    public class VendorRating
    {
        public int Id { get; set; }
        public string VendorEmail { get; set; }
        public string VendorName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime RatingDate { get; set; }

    }
}
