
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Interfaces
{
    public interface IMessageRepository
    {
        Task<List<Chat>> GetMessagesAsync();
        Task<Chat> GetMessageByIdAsync(int id);
        Task<Chat> CreateMessageAsync(Chat message);
        Task<bool> DeleteMessageAsync(int id);
        Task<int> CleanupExpiredMessagesAsync();
        Task<int> CleanupExpiredMessagesAsync(DateTime cutoffDate);
    }
}
