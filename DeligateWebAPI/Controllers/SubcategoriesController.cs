using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubcategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubcategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Subcategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorSubcategory>>> GetAll()
        {
            return Ok(await _context.VendorSubcategories.ToListAsync());
        }

        // GET: api/Subcategories/by-category/5
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<VendorSubcategory>>> GetByCategory(int categoryId)
        {
            var subcategories = await _context.VendorSubcategories
                .Where(sc => sc.VendorCategoryId == categoryId)
                .ToListAsync();

            return Ok(subcategories);
        }

        // GET: api/Subcategories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VendorSubcategory>> GetById(int id)
        {
            var subcategory = await _context.VendorSubcategories.FindAsync(id);

            if (subcategory == null)
                return NotFound();

            return Ok(subcategory);
        }
    }
}
