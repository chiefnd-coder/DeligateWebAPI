using DeligateWebAPI.Data;
using DeligateWebAPI.Interfaces;
using DeligateWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageRepository> _logger;
        private readonly IWebHostEnvironment _environment;

        public MessageRepository(
            ApplicationDbContext context,
            ILogger<MessageRepository> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<List<Chat>> GetMessagesAsync()
        {
            return await _context.Chats
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<Chat> GetMessageByIdAsync(int id)
        {
            return await _context.Chats.FindAsync(id);
        }

        public async Task<Chat> CreateMessageAsync(Chat message)
        {
            message.Timestamp = DateTime.UtcNow;
            _context.Chats.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id)
        {
            var message = await _context.Chats.FindAsync(id);
            if (message == null) return false;

            // Delete associated file if exists
            if (!string.IsNullOrEmpty(message.MediaUrl))
            {
                try
                {
                    await DeleteMediaFile(message.MediaUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete media file for message {MessageId}: {MediaUrl}",
                        id, message.MediaUrl);
                    // Continue with database deletion even if file deletion fails
                }
            }

            _context.Chats.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupExpiredMessagesAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-49);
            return await CleanupExpiredMessagesAsync(cutoffDate);
        }

        public async Task<int> CleanupExpiredMessagesAsync(DateTime cutoffDate)
        {
            try
            {
                // Get expired messages with media files first
                var expiredMessagesWithFiles = await _context.Chats
                    .Where(m => m.Timestamp < cutoffDate && !string.IsNullOrEmpty(m.MediaUrl))
                    .Select(m => new { m.Id, m.MediaUrl })
                    .ToListAsync();

                // Delete associated files
                int filesDeleted = 0;
                foreach (var message in expiredMessagesWithFiles)
                {
                    try
                    {
                        if (await DeleteMediaFile(message.MediaUrl))
                        {
                            filesDeleted++;
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "Failed to delete file for message {MessageId}: {MediaUrl}",
                            message.Id, message.MediaUrl);
                    }
                }

                // Delete messages from database
                var deletedCount = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"Chats\" WHERE \"Timestamp\" < {0}", cutoffDate);

                _logger.LogInformation($"Successfully deleted {deletedCount} expired messages and {filesDeleted} media files");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during message cleanup");
                throw;
            }
        }

        private async Task<bool> DeleteMediaFile(string mediaUrl)
        {
            if (string.IsNullOrEmpty(mediaUrl))
                return false;

            try
            {
                string filePath = GetPhysicalFilePath(mediaUrl);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug($"Deleted file: {filePath}");
                    return true;
                }
                else
                {
                    _logger.LogDebug($"File not found: {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {MediaUrl}", mediaUrl);
                return false;
            }
        }

        private string GetPhysicalFilePath(string mediaUrl)
        {
            // Remove leading slash and convert URL path to physical path
            string relativePath = mediaUrl.TrimStart('/');

            // Handle different upload directories
            if (relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", "Uploads",
                    relativePath.Substring("uploads/".Length));
            }
            else if (relativePath.StartsWith("MediaUploads/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", "MediaUploads",
                    relativePath.Substring("MediaUploads/".Length));
            }
            else
            {
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", relativePath);
            }
        }
    }
}