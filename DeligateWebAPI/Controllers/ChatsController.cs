using DeligateWebAPI.Data; // Adjust namespace as needed
using DeligateWebAPI.Models; // Adjust namespace as needed
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ChatsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Chats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Chat>>> GetChats()
        {
            return await _context.Chats.ToListAsync();
        }


        //public async Task<ActionResult<IEnumerable<Chat>>> GetUserChats(string userId)
        //{
        //    var userChats = await _context.Chats
        //        .Where(c => c.ReceiverId == userId)
        //        .OrderByDescending(c => c.Timestamp)
        //        .ToListAsync();

        //    if (!userChats.Any())
        //    {
        //        return Ok(new List<Chat>()); // Return empty list instead of NotFound
        //    }

        //    return Ok(userChats);
        //}

        // GET: api/Chats/user/{userId} - Get chats for a specific user
        [HttpGet("received/{userId}")]
        public async Task<ActionResult<IEnumerable<Chat>>> GetUserChats(string userId)
        {
            var userChats = await _context.Chats
                .Where(c => c.ReceiverId == userId)
                .OrderBy(c => c.Id) // ✅ Order by ascending
                .ToListAsync();

            if (!userChats.Any())
            {
                return Ok(new List<Chat>());
            }

            return Ok(userChats);
        }

        // GET: api/Chats/user/{userId} - Get chats for a specific user
        [HttpGet("sent/{userId}")]
        public async Task<ActionResult<IEnumerable<Chat>>> GetSentMessages(string userId)
        {
            var userChats = await _context.Chats
                .Where(c => c.SenderId == userId )
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();

            if (!userChats.Any())
            {
                return Ok(new List<Chat>()); // Return empty list instead of NotFound
            }

            return Ok(userChats);
        }

        [HttpGet("unread-count/{userId}")]

        public async Task<ActionResult<int>> GetUnreadCount(string userId)
        {
            try
            {

                var unreadCount = await _context.Chats
          .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

                return unreadCount; // Return raw integer

            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        // GET: api/Chats/conversation/{userId1}/{userId2} - Get conversation between two users
        [HttpGet("conversation/{userId1}/{userId2}")]
        public async Task<ActionResult<IEnumerable<Chat>>> GetConversation(string userId1, string userId2)
        {
            var conversation = await _context.Chats
                .Where(c => (c.SenderId == userId1 && c.ReceiverId == userId2) ||
                           (c.SenderId == userId2 && c.ReceiverId == userId1))
                .OrderBy(c => c.Timestamp)
                .ToListAsync();

            return Ok(conversation);
        }

        // GET: api/Chats/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Chat>> GetChat(int id)
        {
            var chat = await _context.Chats.FindAsync(id);

            if (chat == null)
            {
                return NotFound();
            }

            return chat;
        }

        // PUT: api/Chats/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChat(int id, Chat chat)
        {
            if (id != chat.Id)
            {
                return BadRequest();
            }

            _context.Entry(chat).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChatExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/Chats/markread/{chatId} - Mark a chat as read
        [HttpPut("markread/{chatId}")]
        public async Task<IActionResult> MarkChatAsRead(int chatId)
        {
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null)
            {
                return NotFound();
            }

            chat.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }


        // POST: api/Chats
        [HttpPost]
        public async Task<ActionResult<Chat>> PostChat(Chat chat)
        {
            chat.Timestamp = chat.Timestamp.ToUniversalTime();
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChat", new { id = chat.Id }, chat);
        }

        // POST: api/Chats/upload - Upload chat with file
        [HttpPost("upload")]
        public async Task<ActionResult<Chat>> PostChatWithFile([FromForm] ChatUploadModel model)
        {
            try
            {
                // Add validation
                if (string.IsNullOrEmpty(model.SenderId) || string.IsNullOrEmpty(model.ReceiverId))
                {
                    return BadRequest("SenderId and ReceiverId are required");
                }

                if (!DateTime.TryParseExact(model.Timestamp, "O", null, DateTimeStyles.RoundtripKind, out var timestamp))
                {
                    return BadRequest($"Invalid timestamp format: {model.Timestamp}");
                }

                if (!bool.TryParse(model.IsRead, out var isRead))
                {
                    return BadRequest($"Invalid IsRead value: {model.IsRead}");
                }

                var chat = new Chat
                {
                    SenderId = model.SenderId,
                    ReceiverId = model.ReceiverId,
                    Content = model.Content,
                    Timestamp = timestamp.ToUniversalTime(),
                    IsRead = isRead
                };

                if (model.File != null && model.File.Length > 0)
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }

                    // Set media properties
                    chat.MediaUrl = $"/uploads/{fileName}";
                    chat.MediaType = GetMediaType(model.File.ContentType);
                }

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetChat", new { id = chat.Id }, chat);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error uploading file: {ex.Message}");
            }
        }

        private string GetMediaType(string contentType)
        {
            if (contentType.StartsWith("image/"))
                return "image";
            else if (contentType.StartsWith("video/"))
                return "video";
            else
                return "document";
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChat(int id)
        {
            var chat = await _context.Chats.FindAsync(id);
            if (chat == null)
            {
                return NotFound();
            }

            try
            {
                // Delete associated file if exists
                if (!string.IsNullOrEmpty(chat.MediaUrl))
                {
                    var filePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, chat.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Chats.Remove(chat);
                await _context.SaveChangesAsync();

                return Ok(); // 200 OK
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., to file or monitoring system)
                Console.WriteLine($"Error deleting chat: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the chat.");
            }
        }


        private bool ChatExists(int id)
        {
            return _context.Chats.Any(e => e.Id == id);
        }
    }

    // Model for file upload

    public class ChatUploadModel
    {
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string? Content { get; set; }
        public string Timestamp { get; set; } // Keep as string for form data
        public string IsRead { get; set; }    // Keep as string for form data
        public IFormFile? File { get; set; }
    }
}