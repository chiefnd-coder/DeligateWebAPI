using Microsoft.AspNetCore.SignalR;

namespace DeligateWebAPI.Hubs
{
    public class ChatHub2: Hub
    {
        public async Task SendMessage(string message)
        {
          

            try
            {
                
                await Clients.All.SendAsync("MessageReceived", message);
                Console.WriteLine($"Message succesfully sent");
            }

            catch
            {
                Console.WriteLine(message);
                Console.WriteLine($"Message not sent {message}");
            }
        }
    }
}
