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
    public class DeligateTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DeligateTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DeligateTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeligateTask>>> GetDeligateTasks()
        {
            return await _context.DeligateTasks.ToListAsync();
        }

        // GET: api/DeligateTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DeligateTask>> GetDeligateTask(int id)
        {
            var deligateTask = await _context.DeligateTasks.FindAsync(id);

            if (deligateTask == null)
            {
                return NotFound();
            }

            return deligateTask;
        }

        // PUT: api/DeligateTasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeligateTask(int id, DeligateTask deligateTask)
        {
            if (id != deligateTask.Id)
            {
                return BadRequest();
            }

            _context.Entry(deligateTask).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeligateTaskExists(id))
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

        // POST: api/DeligateTasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DeligateTask>> PostDeligateTask(DeligateTask deligateTask)
        {
            _context.DeligateTasks.Add(deligateTask);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDeligateTask", new { id = deligateTask.Id }, deligateTask);
        }

        // DELETE: api/DeligateTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeligateTask(int id)
        {
            var deligateTask = await _context.DeligateTasks.FindAsync(id);
            if (deligateTask == null)
            {
                return NotFound();
            }

            _context.DeligateTasks.Remove(deligateTask);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeligateTaskExists(int id)
        {
            return _context.DeligateTasks.Any(e => e.Id == id);
        }
    }
}
