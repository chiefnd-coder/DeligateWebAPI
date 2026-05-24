using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
    public class GoogleAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // Add this

        public GoogleAuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // Initialize it
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            // Challenge Google directly
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Explicitly authenticate against the Cookie scheme where Google saved the data
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded) return BadRequest("Google authentication failed.");

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);

            var user = await _context.Register.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {

                user = new Register
                {
                    FullName = fullName,
                    Email = email.ToLowerInvariant(),
                    UserUniqueId = "",
                    Country = "",
                    IsTokenActive = true,
                    Password = "",
                    DeviceToken = "",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Register.Add(user);
                await _context.SaveChangesAsync();
            }


            var token = GenerateYourJwtToken(user);
            return Redirect($"myapp://#token={token}");
        }
        private string GenerateYourJwtToken(Register user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FullName ?? "")
    };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7), // Token valid for 7 days
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
