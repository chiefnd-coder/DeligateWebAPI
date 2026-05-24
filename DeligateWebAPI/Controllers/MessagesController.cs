
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public MessagesController(ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        //[HttpPost("save")]
        //public async Task<IActionResult> SaveMessage([FromBody] SaveMessageRequest request)
        //{
        //    try
        //    {
        //        var message = new Message
        //        {
        //            SenderId = request.SenderId,
        //            ReceiverId = request.ReceiverId,
        //            Content = request.Message,
        //            Timestamp = request.Timestamp.ToUniversalTime(),
        //            IsDelivered = true
        //        };

        //        _context.Messages.Add(message);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { success = true, messageId = message.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}


        [HttpPost("save")]
        public async Task<IActionResult> SaveMessage([FromBody] SaveMessageRequest request)
        {
            try
            {
                var message = new Message
                {
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Message,
                    Timestamp = request.Timestamp.ToUniversalTime(),
                    IsDelivered = true,
                    // Add media properties
                    MediaUrl = request.MediaUrl,
                    MediaType = request.MediaType
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, messageId = message.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("upload-media")]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            try
            {
                var uploads = Path.Combine(_env.WebRootPath, "ChatMediaUploads");
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploads, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok($"/ChatMediaUploads/{uniqueFileName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload error: {ex.Message}");
            }
        }

        [HttpGet("conversations/{userId}")]
        public async Task<IActionResult> GetConversations(string userId)
        {
            try
            {
                // Get all conversations for the user
                var conversations = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Select(g => new
                    {
                        ParticipantId = g.Key,
                        Messages = g.OrderBy(m => m.Timestamp).ToList(),
                        UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                    })
                    .ToListAsync();

                var result = new List<ConversationResponse>();
                string profpicuture = "";
                foreach (var conv in conversations)
                {
                    // Get participant name from Users table
                    var participant = await _context.Register.FirstOrDefaultAsync(u => u.Email == conv.ParticipantId);
                    var profileimg = await _context.VendorRegistration.FirstOrDefaultAsync(u => u.Email == conv.ParticipantId);

                    if(profileimg != null)
                    {
                        profpicuture = profileimg.UploadedPhotoPath;
                    }
                    var conversationResponse = new ConversationResponse
                    {
                        ParticipantId = conv.ParticipantId,
                        ParticipantName = participant?.FullName ?? conv.ParticipantId,
                        ImageProfile = profpicuture,
                        UnreadCount = conv.UnreadCount,
                        Messages = conv.Messages.Select(m => new MessageResponse
                        {
                            Id = m.Id.ToString(),
                            SenderId = m.SenderId,
                            ReceiverId = m.ReceiverId,
                            Message = m.Content,
                            Timestamp = m.Timestamp,
                            IsDelivered = m.IsDelivered,
                            IsRead = m.IsRead,
                            // Add media mapping
                            MediaUrl = m.MediaUrl,
                            MediaType = m.MediaType
                        }).ToList()
                    };

                    result.Add(conversationResponse);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/{participantId}")]
        public async Task<IActionResult> GetMessages(string userId, string participantId, [FromQuery] DateTime? since = null)
        {
            try
            {
                var query = _context.Messages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == participantId) ||
                               (m.SenderId == participantId && m.ReceiverId == userId));

                if (since.HasValue)
                {
                    query = query.Where(m => m.Timestamp > since.Value);
                }

                var messages = await query
                    .OrderBy(m => m.Timestamp)
                   .Select(m => new MessageResponse
                   {
                       Id = m.Id.ToString(),
                       SenderId = m.SenderId,
                       ReceiverId = m.ReceiverId,
                       Message = m.Content,
                       Timestamp = m.Timestamp,
                       IsDelivered = m.IsDelivered,
                       IsRead = m.IsRead,
                       // Add media mapping
                       MediaUrl = m.MediaUrl,
                       MediaType = m.MediaType
                   })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("mark-read/{userId}/{participantId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string userId, string participantId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.SenderId == participantId && m.ReceiverId == userId && !m.IsRead)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, markedCount = messages.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("unread-count/{userId}")]
    
        public async Task<ActionResult<int>> GetUnreadCount(string userId)
        {
            try
            {
                //var unreadCount = await _context.Messages
                //    .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

                //return Ok(new { unreadCount });
                var unreadCount = await _context.Messages
          .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

                return unreadCount; // Return raw integer
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("delete-conversation/{userId}/{participantId}")]
        public async Task<IActionResult> DeleteConversation(string userId, string participantId)
        {
            try
            {
                // Get all messages between these users
                var messages = await _context.Messages
                    .Where(m =>
                        (m.SenderId == userId && m.ReceiverId == participantId) ||
                        (m.SenderId == participantId && m.ReceiverId == userId))
                    .ToListAsync();

                if (messages.Any())
                {
                    _context.Messages.RemoveRange(messages);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, deletedCount = messages.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
