namespace DeligateWebAPI.Models
{
    public class EmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
