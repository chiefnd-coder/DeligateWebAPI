
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorRatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VendorRatingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/VendorRatings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorRating>>> GetVendorRating()
        {
            return await _context.VendorRating.ToListAsync();
        }

        // GET: api/VendorRatings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VendorRating>> GetVendorRating(int id)
        {
            var vendorRating = await _context.VendorRating.FindAsync(id);

            if (vendorRating == null)
            {
                return NotFound();
            }

            return vendorRating;
        }

        [HttpGet("vendor/{vendorEmail}")]
        public async Task<ActionResult<IEnumerable<VendorRating>>> GetVendorRatingsByVendorEmail(string VendorEmail)
        {
            return await _context.VendorRating
                .Where(r => r.VendorEmail == VendorEmail)
                .ToListAsync();
        }

        // PUT: api/VendorTasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVendorTasks(int id, VendorTasks vendorTasks)
        {
            if (id != vendorTasks.Id)
            {
                return BadRequest("ID mismatch");
            }

            var existingTask = await _context.VendorTasks.FindAsync(id);
            if (existingTask == null)
            {
                return NotFound();
            }

            // Only update these specific fields
            existingTask.Status = vendorTasks.Status;
            existingTask.StatusColor = vendorTasks.StatusColor;
            existingTask.Ratings = vendorTasks.Ratings;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                //if (!VendorTasks(id))
                //{
                //    return NotFound();
                //}
                throw;
            }

            return NoContent();
        }



        // POST: api/VendorRatings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<VendorRating>> PostVendorRating(CreateVendorRatingDto ratingDto)
        {
            var rating = new VendorRating
            {
                VendorEmail = ratingDto.VendorEmail,
                VendorName = ratingDto.VendorName,
                Rating = ratingDto.Rating,
                Comment = ratingDto.Comment,
                RatingDate = DateTime.UtcNow
            };

            _context.VendorRating.Add(rating);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVendorRating", new { id = rating.Id }, rating);
        }

        // DELETE: api/VendorRatings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendorRating(int id)
        {
            var vendorRating = await _context.VendorRating.FindAsync(id);
            if (vendorRating == null)
            {
                return NotFound();
            }

            _context.VendorRating.Remove(vendorRating);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VendorRatingExists(int id)
        {
            return _context.VendorRating.Any(e => e.Id == id);
        }
    }
}
