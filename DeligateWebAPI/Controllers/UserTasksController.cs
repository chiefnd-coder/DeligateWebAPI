using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserTasks()
        {
            return await _context.UserTasks.ToListAsync();
        }

        // GET: api/UserTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserTasks>> GetUserTasks(int id)

        {
            var userTasks = await _context.UserTasks.FindAsync(id);

            if (userTasks == null)
            {
                return NotFound();
            }

            return userTasks;
        }

        // GET: api/VendorTasks/ByEmail?email=example@example.com
        [HttpGet("ByVendorEmail")]
        public async Task<ActionResult<IEnumerable<UserTasks>>> GetVendorTaskByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var vendortask = await _context.UserTasks
                                        .Where(v => v.VendorEmail == email)
                                        .ToListAsync();

            if (vendortask == null || !vendortask.Any())
            {
                return NotFound("No vendors task found with the specified email.");
            }

            return Ok(vendortask);
        }


        //[HttpGet("UserCreatedTaskByEmail")]
        //public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserCreatedTaskByEmail(string email)
        //{
        //    return  await _context.UserTasks.FirstOrDefaultAsync(u => u.DelegatorEmail == email);
        //}




        // GET: api/Users/ByUsername/{username}
        [HttpGet("ByUsername/{username}")]
        public async Task<ActionResult<UserTasks>> GetUserTaskByUsername(string username)
        {
            var user = await _context.UserTasks
                .FirstOrDefaultAsync(u => u.DelegatorEmail == username);
            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpGet("ByNonVendorEmail")]
        public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserTaskByNonVendorEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var nonvendortask = await _context.UserTasks
                                        .Where(v => v.NonVendorEmail == email || v.VendorEmail == email)
                                        .ToListAsync();

            if (nonvendortask.Any())
            {
                return Ok(nonvendortask);
            }

            return NotFound("No tasks found with the specified email.");
        }

        //[HttpGet("ByUserEmail")]
        //public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserTaskByEmail(string email)
        //{
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        return BadRequest("Email cannot be empty.");
        //    }

        //    var userregistereddetails = _context.Register
        //                                .Where(v => v.Email == email)
        //                                .FirstOrDefault();

        //    var registeredvendordetails = _context.VendorRegistration
        //                             .Where(v => v.Email == email)
        //                             .FirstOrDefault();

        //    var vendortask = await _context.UserTasks
        //                                .Where(v => v.DeligatedTo == userregistereddetails.FullName || v.DeligatedTo == registeredvendordetails.CompanyName || v.DelegatorEmail == email)
        //                                .ToListAsync();

        //    if (vendortask.Any())
        //    {
        //        return Ok(vendortask);
        //    }

        //    return NotFound("No tasks found with the specified email.");
        //}

        [HttpGet("ByUserEmail")]
        public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserTaskByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var userregistereddetails = _context.Register
                                        .Where(v => v.Email == email)
                                        .FirstOrDefault();

            var ud = userregistereddetails;

            var registeredvendordetails = _context.VendorRegistration
                                     .Where(v => v.Email == email)
                                     .FirstOrDefault();

            var rv = registeredvendordetails;

            // Alternative concise approach using null-conditional operators and string concatenation
            var vendortask = await _context.UserTasks
                .Where(v => v.DelegatorEmail == email ||
                           (userregistereddetails != null && v.DeligatedTo == userregistereddetails.FullName) ||
                           (registeredvendordetails != null && v.DeligatedTo == registeredvendordetails.CompanyName))
                .OrderBy(x => x.Id)  // Ascending order first
                .ToListAsync();
            vendortask.Reverse();  // Reverse in memory

            if (vendortask.Any())
            {
                return Ok(vendortask);
            }

            return NotFound("No tasks found with the specified email.");
        }


        [HttpGet("usertasksbyemailonly")]
        public async Task<ActionResult<IEnumerable<UserTasks>>> GetUserTasks(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

          
            var line = from e in _context.UserTasks
                       where e.DelegatorEmail == email
                       select e;
         
           

         
            if (line.Any())
            {
                return Ok(line);
            }

            return NotFound("No tasks found with the specified email.");
        }


        [HttpGet("MessageCount")]
        public async Task<ActionResult<int>> GetMessageCountByLoggedinUser(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }
            //var nonVendorTaskCount = await _context.UserTasks
            //                            .Where(v => v.NonVendorEmail == email && v.MessageStatus == null || v.VendorEmail == email && v.MessageStatus == null)
            //                            .CountAsync();
            var nonVendorTaskCount = await _context.UserTasks
                               .Where(v => (v.NonVendorEmail == email && v.MessageStatus == null) || (v.VendorEmail == email && v.MessageStatus == null))
                               .CountAsync();

            // Always return the count, even if it's 0
            return Ok(nonVendorTaskCount);
        }


        // PUT: api/UserTasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserTasks(int id, UserTasks userTasks)
        {
            if (id != userTasks.Id)
            {
                return BadRequest();
            }

            _context.Entry(userTasks).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserTasksExists(id))
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


        [HttpPut("remote/{remoteId}")]  // ← add "remote/" prefix to avoid clashing with {id}
        public async Task<IActionResult> PutUserTaskByRemoteId(int remoteId, UserTasks userTasks)
        {
            var existingTask = await _context.UserTasks
                .FirstOrDefaultAsync(t => t.Id == remoteId);

            if (existingTask == null) return NotFound();


            var line = from e in _context.UserTasks
                         where e.Id == remoteId
                         select e;
            var deliners = line.ToList();

            foreach (var y in deliners)
            {
                y.TaskName = userTasks.TaskName;
                y.TaskDetail = userTasks.TaskDetail;
                y.CategoryName = userTasks.CategoryName;
                y.DeligatedTo = userTasks.DeligatedTo;
                y.DelegateStatus = userTasks.DelegateStatus;
                y.DelegatorEmail = userTasks.DelegatorEmail;
                y.DelegatorName = userTasks.DelegatorName;
                y.DelegatorPhoneNumber = userTasks.DelegatorPhoneNumber;
                y.VendorEmail = userTasks.VendorEmail;
                y.VendorName = userTasks.VendorName;
                y.VendorPhoneNumber = userTasks.VendorPhoneNumber;
                y.NonVendorEmail = userTasks.NonVendorEmail;
                y.NonVendorName = userTasks.NonVendorName;
                y.NonVendorPhoneNumber = userTasks.NonVendorPhoneNumber;
                y.Ratings = userTasks.Ratings;
                y.MessageStatus = userTasks.MessageStatus;
                y.VendorTaskId = userTasks.VendorTaskId;
                y.Status = userTasks.Status;
                y.StatusColor = userTasks.StatusColor;
                y.StatusSorting = userTasks.StatusSorting;
                y.StartDate = userTasks.StartDate;
                y.EndDate = userTasks.EndDate;
                y.Date = userTasks.Date;
                y.StartTime = userTasks.StartTime;
                y.EndTime = userTasks.EndTime;
            }



            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }


        // POST: api/UserTasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<UserTasks>> PostUserTasks(UserTasks userTasks)
        //{
        //    _context.UserTasks.Add(userTasks);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetUserTasks", new { id = userTasks.Id }, userTasks);
        //}

        //[HttpPost]
        //public async Task<ActionResult<UserTasks>> PostUserTasks(UserTasks userTasks)
        //{
        //    // Set defaults if not provided
        //    userTasks.Date = DateTime.Now.ToString("yyyy-MM-dd");
        //    userTasks.StartDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    userTasks.EndDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        //    var taskExists = await _context.UserTasks.AnyAsync(r => r.TaskName == "Plant a tree" && r.DelegatorEmail == userTasks.DelegatorEmail);
        //    if (taskExists)
        //    {
        //        return BadRequest(new { message = "Task Exists" });
        //    }


        //    _context.UserTasks.Add(userTasks);
        //    await _context.SaveChangesAsync();
        //    return CreatedAtAction("GetUserTasks", new { id = userTasks.Id }, userTasks);
        //}


        //[HttpPost]
        //public async Task<ActionResult<UserTasks>> PostUserTasks(UserTasks userTasks)
        //{
        //    // Set defaults if not provided
        //    userTasks.Date = DateTime.Now.ToString("yyyy-MM-dd");
        //    userTasks.StartDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    userTasks.EndDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");


        //    var taskExists = await _context.UserTasks.AnyAsync(r =>
        //        r.TaskName == userTasks.TaskName &&
        //        r.DelegatorEmail == userTasks.DelegatorEmail);

        //    if (taskExists)
        //    {
        //        return CreatedAtAction("GetUserTasks", new { id = taskExists.Id }, userTasks);
        //    }

        //    _context.UserTasks.Add(userTasks);
        //    await _context.SaveChangesAsync();
        //    return CreatedAtAction("GetUserTasks", new { id = userTasks.Id }, userTasks);
        //}

        [HttpPost]
        public async Task<ActionResult<UserTasks>> PostUserTasks(UserTasks userTasks)
        {
            userTasks.Date = DateTime.Now.ToString("yyyy-MM-dd");
            userTasks.StartDate = DateTime.Now.ToString("yyyy-MM-dd");
            userTasks.EndDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");

            var existingTask = await _context.UserTasks.FirstOrDefaultAsync(r =>
                r.TaskName == userTasks.TaskName &&
                r.DelegatorEmail == userTasks.DelegatorEmail);

            if (existingTask != null)
            {
                return CreatedAtAction("GetUserTasks", new { id = existingTask.Id }, existingTask); 
            }

            _context.UserTasks.Add(userTasks);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserTasks", new { id = userTasks.Id }, userTasks);
        }


        // DELETE: api/UserTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserTasks(int id)
        {
            var userTasks = await _context.UserTasks.FindAsync(id);
            if (userTasks == null)
            {
                return NotFound();
            }

            _context.UserTasks.Remove(userTasks);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // DELETE: api/UserTasks/5

        [HttpDelete("deletebyremoteid/{remoteId}")]
        public async Task<IActionResult> DeleteUserTaskByRemoteId(int remoteId)
        {
            var existingTask = await _context.UserTasks
                .FirstOrDefaultAsync(t => t.Id == remoteId);

            if (existingTask == null) return NotFound();

            _context.UserTasks.Remove(existingTask);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserTasksExists(int id)
        {
            return _context.UserTasks.Any(e => e.Id == id);
        }
    }
}
