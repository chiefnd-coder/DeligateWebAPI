using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace DeligateWebAPI.Hubs
{
    public class ChatHub3 : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public ChatHub3(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task RegisterUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "UserId cannot be empty");
                return;
            }

            UserConnections[userId] = Context.ConnectionId;
            Console.WriteLine($"User {userId} registered with connection {Context.ConnectionId}");
            await Clients.Others.SendAsync("UserConnected", userId);
        }

        public async Task SendMessage(string senderId, string receiverId, string message,
            string mediaBase64 = null, string mediaType = null, string fileName = null)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                await Clients.Caller.SendAsync("Error", "ReceiverId cannot be empty");
                return;
            }

            Console.WriteLine($"Attempting to send message from {senderId} to {receiverId}");
            Console.WriteLine($"Available connections: {string.Join(", ", UserConnections.Select(kv => $"{kv.Key}:{kv.Value}"))}");

            // Generate unique message ID for tracking
            string messageId = Guid.NewGuid().ToString();

            if (UserConnections.TryGetValue(receiverId, out var connectionId))
            {
                Console.WriteLine($"Found connection {connectionId} for user {receiverId}");

                // Send to the specific receiver with message ID
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message, mediaBase64, mediaType, messageId);

                // Send confirmation back to sender with message ID
                await Clients.Caller.SendAsync("MessageSent", receiverId, message, mediaBase64, mediaType, messageId);
                await Clients.Caller.SendAsync("UserOnline", receiverId);
            }
            else
            {
                Console.WriteLine($"No connection found for user {receiverId}");
                var chat = new Chat
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = message,
                    Timestamp = DateTime.Now.ToUniversalTime(),
                    IsRead = false,
                    MediaUrl = "",
                    MediaBase64 = mediaBase64,
                    MediaType = mediaType,
                    MessageId = messageId // Add MessageId to your Chat model
                };

                try
                {
                    string fullname = "";
                    var reg = from e in _context.Register
                              where e.Email == senderId
                              select e;

                    var regis = reg.ToList();

                    foreach (var y in regis)
                    {
                        fullname = y.FullName;
                    }

                    chat.FullName = fullname;
                    _context.Chats.Add(chat);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed {ex}");
                }

                await Clients.Caller.SendAsync("UserOffline", receiverId, message, fileName, mediaType, messageId);
            }
        }

        // NEW METHOD: Handle message seen acknowledgment
        public async Task MarkMessageAsSeen(string messageId, string senderId, string viewerId)
        {
            Console.WriteLine($"Message {messageId} was seen by {viewerId}");

            // Update database to mark message as read
            try
            {
                var message = _context.Chats.FirstOrDefault(c => c.MessageId == messageId);
                if (message != null)
                {
                    message.IsRead = true;
                    message.SeenAt = DateTime.Now.ToUniversalTime();
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update message read status: {ex}");
            }

            // Notify the original sender that their message was seen
            if (UserConnections.TryGetValue(senderId, out var senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("MessageSeen", messageId, viewerId);
            }
        }

        // NEW METHOD: Mark multiple messages as seen (when user opens a chat)
        public async Task MarkMessagesAsSeenBatch(string viewerId, string senderId)
        {
            try
            {
                // Get all unread messages from senderId to viewerId
                var unreadMessages = _context.Chats
                    .Where(c => c.SenderId == senderId && c.ReceiverId == viewerId && !c.IsRead)
                    .ToList();

                if (unreadMessages.Any())
                {
                    var messageIds = new List<string>();
                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.SeenAt = DateTime.Now.ToUniversalTime();
                        messageIds.Add(msg.MessageId);
                    }

                    await _context.SaveChangesAsync();

                    // Notify sender that messages were seen
                    if (UserConnections.TryGetValue(senderId, out var senderConnectionId))
                    {
                        await Clients.Client(senderConnectionId).SendAsync("MessagesSeenBatch", messageIds, viewerId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to mark messages as seen: {ex}");
            }
        }

        public async Task OnConnectionAsync(string receiverId)
        {
            if (UserConnections.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Caller.SendAsync("UserOnline", receiverId);
            }
            else
            {
                await Clients.Caller.SendAsync("UserNotConnected", receiverId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove user from connections when they disconnect
            var userToRemove = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            UserConnections.TryRemove(userToRemove.Key, out _);
            await Clients.Others.SendAsync("UserDisconnected", userToRemove.Key);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task UnregisterUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            if (UserConnections.TryRemove(userId, out var connectionId))
            {
                Console.WriteLine($"User {userId} explicitly unregistered");
                await Clients.Others.SendAsync("UserDisconnected", userId);
            }
        }
    }
}