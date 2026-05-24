
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.Extensions.FileProviders;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorRegistrationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VendorRegistrationsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;

        }

        // GET: api/VendorRegistration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorRegistration>>> GetVendorRegistrations()
        {
            return await _context.VendorRegistration.ToListAsync();
        }

        // GET: api/VendorRegistration/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VendorRegistration>> GetVendorRegistration(int id)
        {
            var vendorRegistration = await _context.VendorRegistration.FindAsync(id);

            if (vendorRegistration == null)
            {
                return NotFound();
            }

            return vendorRegistration;
        }

        // GET: api/VendorRegistrations/ByEmail?email=example@example.com
        [HttpGet("ByEmail")]
        public async Task<ActionResult<IEnumerable<VendorRegistration>>> GetVendorByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var vendors = await _context.VendorRegistration
                                        .Where(v => v.Email == email)
                                        .ToListAsync();

            if (vendors == null || !vendors.Any())
            {
                return NotFound("No vendors found with the specified email.");
            }

            return Ok(vendors);
        }


        // GET: api/VendorRegistrations/ByCompanyName?companyname=rd plumbing
        [HttpGet("ByCompanyName")]
        public async Task<ActionResult<IEnumerable<VendorRegistration>>> GetVendorByCompanyName(string companyname)
        {
            if (string.IsNullOrEmpty(companyname))
            {
                return BadRequest("Company Name cannot be empty.");
            }

            var vendors = await _context.VendorRegistration
                                        .Where(v => v.CompanyName == companyname)
                                        .ToListAsync();

            if (vendors == null || !vendors.Any())
            {
                return NotFound("No vendors found with the specified companyname.");
            }

            return Ok(vendors);
        }


        // PUT: api/VendorRegistration/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVendorRegistration(int id, VendorRegistration vendorRegistration)
        {
            if (id != vendorRegistration.Id)
            {
                return BadRequest();
            }

            _context.Entry(vendorRegistration).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VendorRegistrationExists(id))
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


        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutVendorRegistration(int id, VendorRegistrationModel model)
        //{
        //    var existingVendor = await _context.VendorRegistration.FindAsync(id);

        //    if (existingVendor == null)
        //        return NotFound();

        //    // Update properties
        //    existingVendor.CompanyName = model.CompanyName;
        //    existingVendor.CompanyRegistrationNumber = model.CompanyRegistrationNumber;
        //    // Update all other properties

        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}

        // POST: api/VendorRegistration
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<VendorRegistration>> PostVendorRegistration(VendorRegistration vendorRegistration)
        {
            _context.VendorRegistration.Add(vendorRegistration);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetVendorRegistration", new { id = vendorRegistration.Id }, vendorRegistration);
        }

        // GET: api/VendorRegistrations/ByPhoneNumber?phonenumber=0810457095
        [HttpGet("ByPhoneNumber")]
        public async Task<ActionResult<IEnumerable<VendorRegistrationRequest>>> GetVendorByPhoneNumber(string phonenumber)
        {
            if (string.IsNullOrEmpty(phonenumber))
            {
                return BadRequest("Phone number cannot be empty.");
            }

            var user = await _context.VendorRegistration
                                        .Where(v => v.PhoneNumber == phonenumber)
                                        .ToListAsync();

            if (!user.Any())
            {
                return NotFound("No person found with the specified phone number.");
            }

            return Ok(user);
        }




        //        [HttpGet("BySelectedService")]
        //        public async Task<ActionResult<IEnumerable<VendorRegistrationRequest>>> GetBySelectedService(
        //        [FromQuery] string SelectedService,
        //        [FromQuery] string SelectedCountry,
        //        [FromQuery] string SearchArea)
        //        {
        //            if (string.IsNullOrEmpty(SelectedService))
        //            {
        //                return BadRequest("SelectedService cannot be empty.");
        //            }

        //            if (string.IsNullOrEmpty(SelectedCountry))
        //            {
        //                return BadRequest("SelectedCountry cannot be empty.");
        //            }

        //            if (string.IsNullOrEmpty(SearchArea))
        //            {
        //                return BadRequest("SearchArea cannot be empty.");
        //            }

        //            // Convert search terms to lowercase for case-insensitive comparison
        //            var selectedServiceLower = SelectedService.ToLower();
        //            var searchAreaLower = SearchArea.ToLower();

        //var vendors = await _context.VendorRegistration
        //.Where(v => v.AreaOfSpecialization != null &&v.AreaOfSpecialization.ToLower().Contains(selectedServiceLower) &&
        //                    (SelectedCountry == "All Countries" || v.Country == SelectedCountry) &&
        //                    v.AreaOfService != null &&
        //                    v.AreaOfService.ToLower().Contains(searchAreaLower))
        //                .ToListAsync();

        //            if (!vendors.Any())
        //            {
        //                return NotFound("No vendors found matching your criteria.");
        //            }

        //            return Ok(vendors);
        //        }


        [HttpGet("BySelectedService")]
        public async Task<ActionResult<IEnumerable<VendorRegistrationRequest>>> GetBySelectedService(
    [FromQuery] string SelectedService,
    [FromQuery] string SelectedCountry,
    [FromQuery] string? SearchArea)  // ✅ Make nullable
        {
            if (string.IsNullOrEmpty(SelectedService))
            {
                return BadRequest("SelectedService cannot be empty.");
            }

            if (string.IsNullOrEmpty(SelectedCountry))
            {
                return BadRequest("SelectedCountry cannot be empty.");
            }

            // ✅ Only require SearchArea if a specific country is selected
            if (SelectedCountry != "All Countries" && string.IsNullOrEmpty(SearchArea))
            {
                return BadRequest("SearchArea is required when a specific country is selected.");
            }


            // ✅ Normalize inputs
            SelectedService = SelectedService.Trim();
            SelectedCountry = SelectedCountry.Trim();
            SearchArea = SearchArea?.Trim();

            // Convert search terms to lowercase for case-insensitive comparison
            var selectedServiceLower = SelectedService.ToLower();
            var searchAreaLower = SearchArea?.ToLower(); // ✅ Handle null

            var vendors = await _context.VendorRegistration
                .Where(v => v.AreaOfSpecialization != null &&
                            v.AreaOfSpecialization.ToLower().Contains(selectedServiceLower) &&
                            (SelectedCountry == "All Countries" || v.Country == SelectedCountry) &&
                            // ✅ Only filter by area if SearchArea is provided
                            (string.IsNullOrEmpty(searchAreaLower) ||
                             (v.AreaOfService != null && v.AreaOfService.ToLower().Contains(searchAreaLower))))
                .ToListAsync();

            if (!vendors.Any())
            {
                return NotFound("No vendors found matching your criteria.");
            }

            return Ok(vendors);
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Create unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                // Get wwwroot path
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                // Define upload path within wwwroot
                var uploadsFolder = Path.Combine(webRootPath, "Uploads");

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the relative path to be stored in the database
                return Ok($"/Uploads/{fileName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        //// DELETE: api/VendorRegistration/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteVendorRegistration(int id)
        //{
        //    var vendorRegistration = await _context.VendorRegistration.FindAsync(id);
        //    if (vendorRegistration == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.VendorRegistration.Remove(vendorRegistration);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendorRegistration(int id)
        {
            var vendor = await _context.VendorRegistration.FindAsync(id);
            if (vendor == null) return NotFound();

            // 1. Identify the physical paths
            // Path.Combine handles different slash styles (/ vs \) automatically
            string idPath = Path.Combine(_environment.WebRootPath, vendor.UploadedIdPath.TrimStart('/'));
            string photoPath = Path.Combine(_environment.WebRootPath, vendor.UploadedPhotoPath.TrimStart('/'));

            // 2. Delete the files if they exist
            if (System.IO.File.Exists(idPath)) System.IO.File.Delete(idPath);
            if (System.IO.File.Exists(photoPath)) System.IO.File.Delete(photoPath);

            // 3. Remove from Database
            _context.VendorRegistration.Remove(vendor);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VendorRegistrationExists(int id)
        {
            return _context.VendorRegistration.Any(e => e.Id == id);
        }
    }
}
