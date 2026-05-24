using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // NEW: GET token by email endpoint
        [HttpGet("GetToken")]
        public async Task<ActionResult<TokenResponse>> GetTokenByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email cannot be empty." });
            }

            var user = await _context.Register.FirstOrDefaultAsync(r => r.Email == email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var tokenResponse = new TokenResponse
            {
                DeviceToken = user.DeviceToken,
                Email = user.Email,
                LastUpdated = user.UpdatedAt,
                Suspension = user.Suspension,
                IsTokenActive = user.IsTokenActive,
            };

            return Ok(tokenResponse);
        }

        // EXISTING ENDPOINTS (keeping your current implementation)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Register>>> GetRegister()
        {
            return await _context.Register.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Register>> GetRegister(int id)
        {
            var register = await _context.Register.FindAsync(id);

            if (register == null)
            {
                return NotFound();
            }

            return register;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRegister(int id, Register register)
        {
            if (id != register.Id)
            {
                return BadRequest();
            }

            _context.Entry(register).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegisterExists(id))
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

        [HttpGet("ByPhoneNumber")]
        public async Task<ActionResult<IEnumerable<RegisterRequest>>> GetUserByPhoneNumber(string phonenumber)
        {
            if (string.IsNullOrEmpty(phonenumber))
            {
                return BadRequest("Phone number cannot be empty.");
            }

            var user = await _context.Register
                                        .Where(v => v.PhoneNumber == phonenumber)
                                        .ToListAsync();

            if (!user.Any())
            {
                return NotFound("No person found with the specified phone number.");
            }

            return Ok(user);
        }

        [HttpGet("Byemail")]
        public async Task<ActionResult<IEnumerable<RegisterRequest>>> GetUserByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be empty.");
            }

            var user = await _context.Register
                                        .Where(v => v.Email == email)
                                        .ToListAsync();

            if (!user.Any())
            {
                return NotFound("No person found with the specified email.");
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<Register>> PostRegister(Register register)
         {
            var emailExists = await _context.Register.AnyAsync(r => r.Email == register.Email);



            if (emailExists)
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // Validate new password (add your validation rules)
            if (string.IsNullOrWhiteSpace(register.Password) || register.Password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            register.Password = BCrypt.Net.BCrypt.HashPassword(register.Password);


            register.CreatedAt = DateTime.UtcNow;

            _context.Register.Add(register);

            var archiveEmailExists = await _context.RegisterArchive.AnyAsync(r => r.Email == register.Email);



            if (!archiveEmailExists)
            {
                var archivedata = new RegisterArchive
                {
                    FullName = register.FullName,
                    Email = register.Email,
                    UserUniqueId = register.UserUniqueId,
                    Country = register.Country,
                    IsTokenActive = true,
                    Password = register.Password,
                    DeviceToken = register.DeviceToken
                };


                _context.RegisterArchive.Add(archivedata);
            }



            try
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetRegister", new { id = register.Id }, register);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Database error.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
            }
        }


        [HttpPost("localregistertransfer")]
        public async Task<ActionResult<Register>> PostLocalRegister(Register register)
        {
            var emailExists = await _context.Register.AnyAsync(r => r.Email == register.Email);
            if (emailExists)
            {
                return BadRequest(new { message = "Email already exists." });
            }
            register.CreatedAt = DateTime.UtcNow;
            register.Id = 0;
            var archivedata = new RegisterArchive
            {
                FullName = register.FullName,
                Email = register.Email,
                UserUniqueId = register.UserUniqueId,
                Country = register.Country,
                IsTokenActive = true,
                Password = register.Password,
                DeviceToken = register.DeviceToken
            };

            _context.Register.Add(register);
            _context.RegisterArchive.Add(archivedata);

            try
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetRegister", new { id = register.Id }, register);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Database error.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
            }
        }

        [HttpPost("Onboarding")]
        public async Task<ActionResult<Onboarding>> NewUserOpenedApp(Onboarding onboarding)
        {
            onboarding.Date = DateTime.UtcNow;
            _context.Onboarding.Add(onboarding);
            await _context.SaveChangesAsync();
            return NoContent();

        }


        [AllowAnonymous]
        [HttpGet("CheckUserExists")]
        public async Task<ActionResult<UserExistsResponse>> CheckUserExists(string email)
        {
            var emailExists = await _context.Register.AnyAsync(r => r.Email == email);
            //var phoneNumberExists = await _context.Register.AnyAsync(r => r.PhoneNumber == phoneNumber);

            return Ok(new UserExistsResponse
            {
                EmailExists = emailExists,
                PhoneNumberExists = false
            });
        }

        // UPDATED: Enhanced TokenUpdate endpoint
        [HttpPost("TokenUpdate")]
        public async Task<IActionResult> UpdateDeviceToken(TokenUpdateRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { message = "Email is required to update device token." });
            }

            if (string.IsNullOrEmpty(request.DeviceToken))
            {
                return BadRequest(new { message = "Device token cannot be empty." });
            }

            var user = await _context.Register.FirstOrDefaultAsync(r => r.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Update device token and timestamp
            user.DeviceToken = request.DeviceToken;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsTokenActive = request.IsTokenActive;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Device token updated successfully.",
                    deviceToken = user.DeviceToken,
                    updatedAt = user.UpdatedAt,
                    isTokenActive = user.IsTokenActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating device token: {ex.Message}" });
            }
        }


        [HttpPut("UpdateSuspension")]
        public async Task<IActionResult> UpdateSuspension(SuspensionUpdateRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            if (string.IsNullOrEmpty(request.Suspension))
            {
                return BadRequest(new { message = "Suspension status is required." });
            }

            var user = await _context.Register.FirstOrDefaultAsync(r => r.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Update suspension status and timestamp
            user.Suspension = request.Suspension;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Suspension status updated successfully.",
                    email = user.Email,
                    suspension = user.Suspension,
                    updatedAt = user.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating suspension status: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegister(int id)
        {
            var register = await _context.Register.FindAsync(id);
            if (register == null)
            {
                return NotFound();
            }


            var deleteddata = new DeletedAccount
            {
                FullName = register.FullName,
                Email = register.Email,
                UserUniqueId = register.UserUniqueId,
                Country = register.Country,
                IsTokenActive = false,
                Password = register.Password,
                DeviceToken = register.DeviceToken
            };


            _context.DeletedAccount.Add(deleteddata);

            _context.Register.Remove(register);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("ByEmail/{email}")]
        public async Task<IActionResult> DeleteRegisterByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email cannot be empty." });
            }

            var register = await _context.Register.FirstOrDefaultAsync(r => r.Email == email);
            if (register == null)
            {
                return NotFound(new { message = "User not found." });
            }

            else
            {

                var deleteddata = new DeletedAccount
                {
                    FullName = register.FullName,
                    Email = register.Email,
                    UserUniqueId = register.UserUniqueId,
                    Country = register.Country,
                    IsTokenActive = false,
                    Password = register.Password,
                    DeviceToken = register.DeviceToken
                };


                _context.DeletedAccount.Add(deleteddata);
                _context.Register.Remove(register);
                await _context.SaveChangesAsync();
            }

            //var usertask = await _context.UserTasks.FirstOrDefaultAsync(r => r.DelegatorEmail == email || r.VendorEmail == email);
            //if (usertask != null)
            //{
            //    _context.UserTasks.Remove(usertask);
            //    await _context.SaveChangesAsync();
            //}


            //var chats = await _context.Chats.FirstOrDefaultAsync(r => r.SenderId == email || r.ReceiverId == email);
            //if (chats != null)
            //{
            //    _context.Chats.Remove(chats);
            //    await _context.SaveChangesAsync();
            //}

            //var vendorregistration = await _context.VendorRegistration.FirstOrDefaultAsync(r => r.Email == email);
            //if (vendorregistration != null)
            //{
            //    _context.VendorRegistration.Remove(vendorregistration);
            //    await _context.SaveChangesAsync();
            //}


            //var vendortask = await _context.VendorTasks.FirstOrDefaultAsync(r => r.DelegatorEmail == email || r.VendorEmail == email);
            //if (vendortask != null)
            //{
            //    _context.VendorTasks.Remove(vendortask);
            //    await _context.SaveChangesAsync();
            //}

            // 1. Delete all UserTasks for this email
            var userTasks = await _context.UserTasks
                .Where(r => r.DelegatorEmail == email || r.VendorEmail == email)
                .ToListAsync();

            if (userTasks.Any())
            {
                _context.UserTasks.RemoveRange(userTasks);
            }

            // 2. Delete all Chats for this email
            var chats = await _context.Chats
                .Where(r => r.SenderId == email || r.ReceiverId == email)
                .ToListAsync();

            if (chats.Any())
            {
                _context.Chats.RemoveRange(chats);
            }

            // 3. Repeat this pattern for VendorRegistration and VendorTasks
            var vendorTasks = await _context.VendorTasks
                .Where(r => r.DelegatorEmail == email || r.VendorEmail == email)
                .ToListAsync();

            if (vendorTasks.Any())
            {
                _context.VendorTasks.RemoveRange(vendorTasks);
            }

            // Final Save
            await _context.SaveChangesAsync();


            return Ok(new { message = "Account deleted successfully." });
        }


        private bool RegisterExists(int id)
        {
            return _context.Register.Any(e => e.Id == id);
        }

        // Inner class for token update requests
        public class TokenUpdateRequest
        {
            public string Email { get; set; }
            public string DeviceToken { get; set; }
            public bool IsTokenActive { get; set; }
        }


        public class SuspensionUpdateRequest
        {
            public string Email { get; set; }
            public string Suspension { get; set; } // "Suspended" or "Active"
        }
    }
}