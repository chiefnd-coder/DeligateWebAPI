using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatContext _context;

        public ChatController(ChatContext context)
        {
            _context = context;
        }

        [HttpGet("messages")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetMessages(int skip = 0, int take = 50)
        {
            var messages = await _context.ChatMessages
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("messages/room/{roomId}")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetRoomMessages(string roomId, int skip = 0, int take = 50)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.Timestamp)
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("users/online")]
        public async Task<ActionResult<IEnumerable<User>>> GetOnlineUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsOnline)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }
}
