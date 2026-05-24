using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VendorTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/VendorTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorTasks>>> GetVendorTasks()
        {
            return await _context.VendorTasks.ToListAsync();
        }

        // GET: api/VendorTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VendorTasks>> GetVendorTasks(int id)
        {
            var vendorTasks = await _context.VendorTasks.FindAsync(id);

            if (vendorTasks == null)
            {
                return NotFound();
            }

            return vendorTasks;
        }


        // GET: api/VendorTasks/ByEmail?email=example@example.com
        [HttpGet("ByEmail")]
        public async Task<ActionResult<IEnumerable<VendorTasks>>> GetVendorTaskByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var vendortask = await _context.VendorTasks
                                        .Where(v => v.VendorEmail == email)
                                        .ToListAsync();

            if (vendortask == null || !vendortask.Any())
            {
                return NotFound("No vendors task found with the specified email.");
            }

            return Ok(vendortask);
        }

        // PUT: api/VendorTasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVendorTasks(int id, VendorTasks vendorTasks)
        {
            if (id != vendorTasks.Id)
            {
                return BadRequest();
            }

            _context.Entry(vendorTasks).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VendorTasksExists(id))
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

        // POST: api/VendorTasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<VendorTasks>> PostVendorTasks(VendorTasks vendorTasks)
        {
            _context.VendorTasks.Add(vendorTasks);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVendorTasks", new { id = vendorTasks.Id }, vendorTasks);
        }

        // DELETE: api/VendorTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendorTasks(int id)
        {
            var vendorTasks = await _context.VendorTasks.FindAsync(id);
            if (vendorTasks == null)
            {
                return NotFound();
            }

            _context.VendorTasks.Remove(vendorTasks);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VendorTasksExists(int id)
        {
            return _context.VendorTasks.Any(e => e.Id == id);
        }
    }
}
