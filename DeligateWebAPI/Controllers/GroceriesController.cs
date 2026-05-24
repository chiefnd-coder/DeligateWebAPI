using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroceriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GroceriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Groceries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Grocery>>> GetGroceries()
        {
            return await _context.Groceries.ToListAsync();
        }

        // GET: api/Groceries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Grocery>> GetGrocery(int id)
        {
            var grocery = await _context.Groceries.FindAsync(id);

            if (grocery == null)
            {
                return NotFound();
            }

            return grocery;
        }


        [HttpGet("received/{userId}")]
        public async Task<ActionResult<IEnumerable<Grocery>>> GetUserGrocery(string userId)
        {
            var userGrocery = await _context.Groceries
                .Where(c => c.ToUserName == userId)
                .OrderBy(c => c.Id) // ✅ Order by ascending
                .ToListAsync();

            if (!userGrocery.Any())
            {
                return Ok(new List<Chat>());
            }

            return Ok(userGrocery);
        }


        [HttpGet("grocerysharedwithother/{userId}")]
        public async Task<ActionResult<IEnumerable<Grocery>>> GetSharedUserGrocery(string userId)
        {
            var userGrocery = await _context.Groceries
                .Where(c => c.FromUserName == userId)
                .OrderBy(c => c.Id) // ✅ Order by ascending
                .ToListAsync();

            if (!userGrocery.Any())
            {
                return Ok(new List<Chat>());
            }

            return Ok(userGrocery);
        }

        // PUT: api/Groceries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrocery(int id, Grocery grocery)
        {
            if (id != grocery.Id)
            {
                return BadRequest();
            }

            _context.Entry(grocery).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GroceryExists(id))
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

        //[HttpPost]
        //public async Task<ActionResult<List<Grocery>>> PostGrocery(List<Grocery> groceries)
        //{
        //    if (groceries == null || !groceries.Any())
        //        return BadRequest("No groceries provided.");

        //    // Extract the combinations to check (evaluate in-memory first)
        //    var groceriesToCheck = groceries
        //        .Select(g => new { g.GroceryName, g.FromUserName })
        //        .ToList();

        //    // Query database for existing groceries
        //    var existingGroceries = await _context.Groceries
        //        .Select(e => new { e.GroceryName, e.FromUserName })
        //        .ToListAsync();

        //    var addedGroceries = new List<Grocery>();

        //    foreach (var grocery in groceries)
        //    {
        //        // Check if this grocery already exists (in-memory comparison)
        //        bool isDuplicate = existingGroceries.Any(e =>
        //            e.GroceryName == grocery.GroceryName &&
        //            e.FromUserName == grocery.FromUserName && e.ToUserName == grocery.ToUserName);

        //        if (!isDuplicate)
        //        {
        //            var newGrocery = new Grocery
        //            {
        //                GroceryName = grocery.GroceryName,
        //                FromUserName = grocery.FromUserName,
        //                ToUserName = grocery.ToUserName,
        //                Status = grocery.Status,
        //                SharedId = grocery.SharedId,
        //            };

        //            _context.Groceries.Add(newGrocery);
        //            addedGroceries.Add(newGrocery);
        //        }
        //    }

        //    if (addedGroceries.Any())
        //    {
        //        await _context.SaveChangesAsync();
        //        return CreatedAtAction("GetGrocery", new { id = addedGroceries.First().Id }, addedGroceries);
        //    }

        //    return Ok("All records already existed. No new records added.");
        //}



        [HttpPost]
        public async Task<ActionResult<List<Grocery>>> PostGrocery(List<Grocery> groceries)
        {
            if (groceries == null || !groceries.Any())
                return BadRequest("No groceries provided.");

            // Query database for existing groceries - Include ToUserName in the selection
            var existingGroceries = await _context.Groceries
                .Select(e => new { e.GroceryName, e.FromUserName, e.ToUserName })
                .ToListAsync();

            var addedGroceries = new List<Grocery>();

            foreach (var grocery in groceries)
            {
                // Check if this grocery already exists (in-memory comparison)
                bool isDuplicate = existingGroceries.Any(e =>
                    e.GroceryName == grocery.GroceryName &&
                    e.FromUserName == grocery.FromUserName &&
                    e.ToUserName == grocery.ToUserName);

                if (!isDuplicate)
                {
                    var newGrocery = new Grocery
                    {
                        GroceryName = grocery.GroceryName,
                        FromUserName = grocery.FromUserName,
                        ToUserName = grocery.ToUserName,
                        Status = grocery.Status,
                        SharedId = 0,
                    };

                    _context.Groceries.Add(newGrocery);
                    addedGroceries.Add(newGrocery);
                }
            }

            if (addedGroceries.Any())
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetGrocery", new { id = addedGroceries.First().Id }, addedGroceries);
            }

            return Ok("All records already existed. No new records added.");
        }

        [HttpPost("shared")]
        public async Task<IActionResult> UpdateSharedGrocery([FromQuery] int id, [FromQuery] string? status)
        {
            // If status is empty/null, decide if you want to allow it
            string finalStatus = status ?? "";

            var groceries = await _context.Groceries
                .Where(e => e.Id == id)
                .ToListAsync(); // Use Async version for better performance

            if (!groceries.Any()) return NotFound();

            foreach (var g in groceries)
            {
                g.Status = finalStatus;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("EmailAndGroceryName")]
        public async Task<IActionResult> UpdateEmailAndGroceryName([FromQuery] string name, [FromQuery] string? email, [FromQuery] string? status)
        {
            string finalStatus = status ?? "";

            var groceries = await _context.Groceries
                .Where(e => e.GroceryName == name && e.FromUserName == email)
                .ToListAsync(); // Use Async version for better performance

            if (!groceries.Any()) return NotFound();

            foreach (var g in groceries)
            {
                g.Status = status;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }



        // DELETE: api/Groceries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrocery(int id)
        {
            var groc = await _context.Groceries.FindAsync(id);
            if (groc == null)
            {
                return NotFound();
            }

            try
            {
                _context.Groceries.Remove(groc);
                await _context.SaveChangesAsync();

                return Ok(); // 200 OK
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., to file or monitoring system)
                Console.WriteLine($"Error deleting grocery: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the grocery.");
            }
        }

        // DELETE: api/Groceries/shared/7
        [HttpDelete("shared/{id}")]
        public async Task<IActionResult> DeleteGroceriesBySharedId(int id)
        {
            var groceries = await _context.Groceries
                .Where(e => e.Id == id)
                .ToListAsync();

            if (!groceries.Any())
            {
                return NotFound();
            }

            try
            {
                _context.Groceries.RemoveRange(groceries);
                await _context.SaveChangesAsync();

                return NoContent(); // 204
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting groceries: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting groceries.");
            }
        }





        [HttpGet("GroceryCount")]
        public async Task<ActionResult> GetGroceryCountByLoggedinUser(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email cannot be empty.");


            // Get unique senders
            var uniqueSenders = await _context.Groceries
                .Where(g => g.ToUserName == email && g.FromUserName != email)
                .Select(g => g.FromUserName)
                .Distinct()
                .ToListAsync();

            var result = new
            {
                UniqueSenderCount = uniqueSenders.Count,
                UniqueSenders = uniqueSenders
            };

            return Ok(result);
        }

        // PATCH: api/Groceries/5/sharedid
        [HttpPatch("{id}/sharedid")]
        public async Task<IActionResult> UpdateSharedId(int id, [FromBody] int sharedId)
        {
            var grocery = await _context.Groceries.FindAsync(id);

            if (grocery == null)
            {
                return NotFound();
            }

            grocery.SharedId = sharedId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Groceries/5/sharedid
        [HttpGet("{id}/sharedid")]
        public async Task<ActionResult<int>> GetSharedId(int id)
        {
            var grocery = await _context.Groceries.FindAsync(id);

            if (grocery == null)
            {
                return NotFound();
            }

            return Ok(grocery.SharedId);
        }

        // GET: api/Groceries/sharedid/byemail?email=user@example.com
        [HttpGet("sharedid/byemail")]
        public async Task<ActionResult<int>> GetSharedIdByEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var grocery = await _context.Groceries
                .FirstOrDefaultAsync(g => g.ToUserName == email);

          

            if (grocery == null)
            {
                return Ok(0);  // Return default value of 1
            }

            if (grocery.SharedId == 1)
            {
                return Ok(0);  // Return default value of 1
            }

            else if (grocery.SharedId == 0)
            {
                return Ok(1);
            }

            else 
            {
                return Ok(0);
            }
        }

        private bool GroceryExists(int id)
        {
            return _context.Groceries.Any(e => e.Id == id);
        }
    }
}
