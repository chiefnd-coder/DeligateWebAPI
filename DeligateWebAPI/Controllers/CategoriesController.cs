using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorCategory>>> GetAll()
        {
            return Ok(await _context.VendorCategories.ToListAsync());
        }

        // GET: api/Categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VendorCategory>> GetById(int id)
        {
            var category = await _context.VendorCategories.FindAsync(id);

            if (category == null)
                return NotFound();

            return Ok(category);
        }
    }
}
