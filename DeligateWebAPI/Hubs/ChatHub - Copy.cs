using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace DeligateWebAPI.Hubs
{
    public class ChatHub : Hub
    {
        // Use ConcurrentDictionary for thread safety
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public async Task RegisterUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "UserId cannot be empty");
                return;
            }

            UserConnections[userId] = Context.ConnectionId;

            // Log for debugging
            Console.WriteLine($"User {userId} registered with connection {Context.ConnectionId}");

            await Clients.Others.SendAsync("UserConnected", userId);
        }

        public async Task SendMessage(string senderId, string receiverId, string message, string mediaUrl = "", string mediaType = "")
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                await Clients.Caller.SendAsync("Error", "ReceiverId cannot be empty");
                return;
            }

            // Debug logging
            Console.WriteLine($"Attempting to send message from {senderId} to {receiverId}");
            Console.WriteLine($"Available connections: {string.Join(", ", UserConnections.Select(kv => $"{kv.Key}:{kv.Value}"))}");

            if (UserConnections.TryGetValue(receiverId, out var connectionId))
            {
                Console.WriteLine($"Found connection {connectionId} for user {receiverId}");

                // Send to the specific receiver with media info
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message, mediaUrl, mediaType);
            }
            else
            {
                Console.WriteLine($"No connection found for user {receiverId}");
                // Optionally notify sender that recipient is offline
                await Clients.Caller.SendAsync("UserOffline", receiverId);
            }

            // Also send back to sender for confirmation
            await Clients.Caller.SendAsync("MessageSent", receiverId, message, mediaUrl, mediaType);
        }

        public async Task SendMediaMessage(string senderId, string receiverId, string mediaUrl, string mediaType, string caption = null)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                await Clients.Caller.SendAsync("Error", "ReceiverId cannot be empty");
                return;
            }

            Console.WriteLine($"Attempting to send media from {senderId} to {receiverId}");

            if (UserConnections.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMediaMessage", senderId, mediaUrl, mediaType, caption);
            }
            else
            {
                Console.WriteLine($"No connection found for user {receiverId}");
                await Clients.Caller.SendAsync("UserOffline", receiverId);
            }

            await Clients.Caller.SendAsync("MediaMessageSent", receiverId, mediaUrl, mediaType, caption);
        }

        public override async Task OnConnectedAsync()
        {
            // Try to get userId from query string
            var userId = Context.GetHttpContext()?.Request.Query["userId"].FirstOrDefault();

            Console.WriteLine($"New connection: {Context.ConnectionId}, UserId from query: {userId}");

            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                Console.WriteLine($"Auto-registered user {userId} with connection {Context.ConnectionId}");
                await Clients.Others.SendAsync("UserConnected", userId);
            }
            else
            {
                Console.WriteLine("No userId in query string - user must call RegisterUser manually");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Connection {Context.ConnectionId} disconnected");

            // Find and remove the disconnected user
            var disconnectedUser = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!disconnectedUser.Equals(default(KeyValuePair<string, string>)))
            {
                UserConnections.TryRemove(disconnectedUser.Key, out _);
                Console.WriteLine($"Removed user {disconnectedUser.Key} from connections");
                await Clients.All.SendAsync("UserDisconnected", disconnectedUser.Key);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Helper method to check connection status
        public async Task CheckConnection()
        {
            var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            await Clients.Caller.SendAsync("ConnectionStatus", userId, Context.ConnectionId);
        }

        // Get all online users (for debugging)
        public async Task GetOnlineUsers()
        {
            var onlineUsers = UserConnections.Keys.ToList();
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }
    }
}