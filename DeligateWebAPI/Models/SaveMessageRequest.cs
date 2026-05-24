namespace DeligateWebAPI.Models
{
    public class SaveMessageRequest
    {
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string MediaUrl { get; set; }
        public string MediaType { get; set; }
    }

    public class GetMessagesRequest
    {
        public string UserId { get; set; }
        public string? ParticipantId { get; set; } // Optional - for specific conversation
        public DateTime? Since { get; set; } // Optional - for incremental sync
    }

    //public class MessageResponse
    //{
    //    public string Id { get; set; }
    //    public string SenderId { get; set; }
    //    public string ReceiverId { get; set; }
    //    public string Message { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public bool IsDelivered { get; set; }
    //    public bool IsRead { get; set; }
    //}


    public class MessageResponse
    {
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsRead { get; set; }
        // Add media properties
        public string MediaUrl { get; set; }
        public string MediaType { get; set; }
    }

    public class ConversationResponse
    {
        public string ParticipantId { get; set; }
        public string ParticipantName { get; set; }

        public string ImageProfile { get; set; }
        public List<MessageResponse> Messages { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
