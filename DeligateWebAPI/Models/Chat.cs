namespace DeligateWebAPI.Models
{
    public class Chat
    {

        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; } = false;
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        public string? MediaBase64 { get; set; }
        public string? FullName { get; set; }
        public string? MessageId { get; set; }
        public DateTime? SeenAt { get; set; }


        }
}
