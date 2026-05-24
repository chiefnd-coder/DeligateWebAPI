namespace DeligateWebAPI.Models
{
    public class Grocery
    {
        public int Id { get; set; }
        public string GroceryName { get; set; }
        public string ToUserName { get; set; }
        public string FromUserName { get; set; }
        public string? Status { get; set; }
        public int SharedId { get; set; }

    }
}
