using DeligateWebAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeligateWebAPI.Services
{
    public class MessageCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageCleanupService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check daily
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(49); // 7 weeks

        public MessageCleanupService(
            IServiceProvider serviceProvider,
            ILogger<MessageCleanupService> logger,
            IWebHostEnvironment environment)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _environment = environment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredMessagesAndFiles();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during message cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Wait before retrying
                }
            }
        }

        private async Task CleanupExpiredMessagesAndFiles()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);

            try
            {
                // Step 1: Get expired messages with media files first
                var expiredMessagesWithFiles = await context.Chats
                    .Where(m => m.Timestamp < cutoffDate && !string.IsNullOrEmpty(m.MediaUrl))
                    .Select(m => new { m.Id, m.MediaUrl })
                    .ToListAsync();

                _logger.LogInformation($"Found {expiredMessagesWithFiles.Count} expired messages with media files");

                // Step 2: Delete associated files
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
                        // Continue with other files even if one fails
                    }
                }

                _logger.LogInformation($"Successfully deleted {filesDeleted} media files");

                // Step 3: Delete expired messages from database
                var deletedCount = await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"Chats\" WHERE \"Timestamp\" < {0}", cutoffDate);

                if (deletedCount > 0)
                {
                    _logger.LogInformation($"Deleted {deletedCount} expired messages older than {cutoffDate:yyyy-MM-dd}");
                }
                else
                {
                    _logger.LogInformation("No expired messages found for cleanup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired messages and files");

                // Fallback to EF Core approach if SQL fails
                try
                {
                    await CleanupUsingEntityFramework(cutoffDate, context);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Both SQL and EF Core cleanup methods failed");
                    throw;
                }
            }
        }

        private async Task CleanupUsingEntityFramework(DateTime cutoffDate, ApplicationDbContext context)
        {
            var expiredMessages = await context.Chats
                .Where(m => m.Timestamp < cutoffDate)
                .ToListAsync();

            if (!expiredMessages.Any()) return;

            // Delete files first
            int filesDeleted = 0;
            foreach (var message in expiredMessages.Where(m => !string.IsNullOrEmpty(m.MediaUrl)))
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

            // Delete database records
            context.Chats.RemoveRange(expiredMessages);
            await context.SaveChangesAsync();

            _logger.LogInformation($"Deleted {expiredMessages.Count} expired messages and {filesDeleted} files using EF Core fallback");
        }

        private async Task<bool> DeleteMediaFile(string mediaUrl)
        {
            if (string.IsNullOrEmpty(mediaUrl))
                return false;

            try
            {
                // Extract file path from URL
                // URL format: "/uploads/c43cdd96-5272-45f8-ac30-8340148d373b.pdf"
                // or "/MediaUploads/filename.ext"
                string filePath = GetPhysicalFilePath(mediaUrl);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug($"Deleted file: {filePath}");
                    return true;
                }
                else
                {
                    _logger.LogDebug($"File not found (may have been already deleted): {filePath}");
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
                // Path: wwwroot/Uploads/filename
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", "Uploads",
                    relativePath.Substring("uploads/".Length));
            }
            else if (relativePath.StartsWith("MediaUploads/", StringComparison.OrdinalIgnoreCase))
            {
                // Path: wwwroot/MediaUploads/filename
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", "MediaUploads",
                    relativePath.Substring("MediaUploads/".Length));
            }
            else
            {
                // Default to wwwroot + relative path
                return Path.Combine(_environment.WebRootPath ?? "wwwroot", relativePath);
            }
        }
    }
}