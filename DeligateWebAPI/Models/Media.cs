using System.ComponentModel.DataAnnotations;

namespace DeligateWebAPI.Models
{
    public class Media
    {
        [Key]
        public int Id { get; set; }
        public string? ImagePath { get; set; }  // Nullable
        public string? VideoPath { get; set; }  // Nullable
        public string UserName { get; set; }
        public string? ThumbnailPath { get; set; }

        // Helper properties
        public bool IsImage => !string.IsNullOrEmpty(ImagePath);
        public bool IsVideo => !string.IsNullOrEmpty(VideoPath);
        public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailPath);
    }
}
