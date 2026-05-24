namespace DeligateWebAPI.Models
{
    //public class Message
    //{
    //    public int Id { get; set; }
    //    public string SenderId { get; set; }
    //    public string ReceiverId { get; set; }
    //    public string Content { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public bool IsDelivered { get; set; } = false;
    //    public bool IsRead { get; set; } = false;
    //}

    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDelivered { get; set; } = false;
        public bool IsRead { get; set; } = false;
        // Add media properties
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
    }

}
