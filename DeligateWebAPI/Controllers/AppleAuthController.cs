using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppleAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AppleAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("login-apple")]
        public IActionResult LoginApple()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("AppleCallback") };
            return Challenge(properties, "Apple");
        }

        [HttpGet("apple-callback")]
        [HttpPost("apple-callback")]
        public async Task<IActionResult> AppleCallback()
        {
            try
            {
                var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!authResult.Succeeded) return BadRequest("Apple auth failed.");

                var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
                var fullName = authResult.Principal.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrEmpty(email))
                {
                    var sub = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    email = $"{sub}@apple.placeholder.com";
                }

                var user = await _context.Register.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new Register
                    {
                        FullName = fullName ?? "Apple User",
                        Email = email.ToLowerInvariant(),
                        UserUniqueId = null, // Intentionally null per your requirement
                        IsTokenActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Register.Add(user);
                    await _context.SaveChangesAsync();
                }

                var token = GenerateYourJwtToken(user);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Redirect($"myapp://#token={token}");
            }
            //catch (Exception ex)
            //{
            //    return Content($"Callback Error: {ex.Message}");
            //}

            catch (Exception ex)
            {
                // Redirect back to the app with an error parameter instead of showing raw text
                return Redirect("myapp://#error=auth_failed");
            }
        }

        [HttpPost("apple-token")]
        public async Task<IActionResult> VerifyNativeAppleToken([FromBody] AppleTokenRequest request)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(request.IdentityToken);

                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var appleUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? request.UserId;

                if (string.IsNullOrEmpty(email))
                    email = $"{appleUserId}@apple.placeholder.com";

                var user = await _context.Register.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new Register
                    {
                        FullName = (!string.IsNullOrEmpty(request.firstName))
                                   ? $"{request.firstName} {request.lastName}".Trim()
                                   : "Apple User",
                        Email = email.ToLowerInvariant(),
                        UserUniqueId = null, // Intentionally null
                        IsTokenActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Register.Add(user);
                    await _context.SaveChangesAsync();
                }

                var token = GenerateYourJwtToken(user);
                return Ok(new { token = token });
            }
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            //}
            catch (Exception ex)
            {
                // 1. Log the error to your server logs
                // _logger.LogError(ex, "Apple Auth failed"); 

                // 2. Return a generic error message to the app
                return StatusCode(500, new { error = "An internal error occurred during authentication." });
            }
        }

        private string GenerateYourJwtToken(Register user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.FullName ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class AppleTokenRequest
        {
            public string IdentityToken { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string? firstName { get; set; }
            public string? lastName { get; set; }
        }
    }
}