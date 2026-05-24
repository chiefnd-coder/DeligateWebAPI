using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;

        }
        // GET: api/Services
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetVendorServices()
        {
            return await _context.Services.ToListAsync();
        }

        // GET api/Services/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Service>> GetServiceById(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return service;
        }

        [HttpGet("by-subcategory/{subcategoryId}")]
        public async Task<ActionResult<IEnumerable<Service>>> GetServicesBySubcategory(int subcategoryId)
        {
            var services = await _context.Services
                .Where(s => s.VendorSubcategoryId == subcategoryId)
                .ToListAsync();

            return Ok(services);
        }

        // GET: api/Services/by-category/5
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Service>>> GetServicesByCategory(int categoryId)
        {
            var services = await _context.Services
                .Where(s => s.VendorCategoryId == categoryId)
                .ToListAsync();

            return Ok(services);
        }

    }
}
