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
    public class PeoplesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PeoplesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Peoples
        [HttpGet]
        public async Task<ActionResult<IEnumerable<People>>> GetPeoples([FromQuery] int? DesignationId)
        {
            // Start with the base query
            var query = _context.Peoples.AsQueryable();

            // Apply filter if DesignationId is provided
            if (DesignationId.HasValue)
            {
                query = query.Where(p => p.DesignationId == DesignationId.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/Peoples/5
        [HttpGet("{id}")]
        public async Task<ActionResult<People>> GetPeople(int id)
        {
            var people = await _context.Peoples.FindAsync(id);

            if (people == null)
            {
                return NotFound();
            }

            return people;
        }

        // PUT: api/Peoples/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPeople(int id, People people)
        {
            if (id != people.Id)
            {
                return BadRequest();
            }

            _context.Entry(people).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PeopleExists(id))
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

        // POST: api/Peoples
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<People>> PostPeople(People people)
        {
            _context.Peoples.Add(people);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPeople", new { id = people.Id }, people);
        }

        // DELETE: api/Peoples/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePeople(int id)
        {
            var people = await _context.Peoples.FindAsync(id);
            if (people == null)
            {
                return NotFound();
            }

            _context.Peoples.Remove(people);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PeopleExists(int id)
        {
            return _context.Peoples.Any(e => e.Id == id);
        }
    }
}
