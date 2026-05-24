using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserTrackersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserTrackersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserTrackers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserTracker>>> GetUserTracker()
        {
            return await _context.UserTracker.ToListAsync();
        }

        // GET: api/UserTrackers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserTracker>> GetUserTracker(int id)
        {
            var userTracker = await _context.UserTracker.FindAsync(id);

            if (userTracker == null)
            {
                return NotFound();
            }

            return userTracker;
        }

        // PUT: api/UserTrackers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserTracker(int id, UserTracker userTracker)
        {
            if (id != userTracker.Id)
            {
                return BadRequest();
            }

            _context.Entry(userTracker).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserTrackerExists(id))
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

        // POST: api/UserTrackers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserTracker>> PostUserTracker(UserTracker userTracker)
        {
            _context.UserTracker.Add(userTracker);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserTracker", new { id = userTracker.Id }, userTracker);
        }

        // DELETE: api/UserTrackers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserTracker(int id)
        {
            var userTracker = await _context.UserTracker.FindAsync(id);
            if (userTracker == null)
            {
                return NotFound();
            }

            _context.UserTracker.Remove(userTracker);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserTrackerExists(int id)
        {
            return _context.UserTracker.Any(e => e.Id == id);
        }
    }
}
