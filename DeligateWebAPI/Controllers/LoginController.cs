using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public LoginController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

  
        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return BadRequest("Email and password are required.");
                }

                var user = _context.Register.FirstOrDefault(u => u.Email == loginRequest.Email);
                if (user == null)
                {
                    return Unauthorized("User not found.");
                }

                if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
                {
                    return Unauthorized("Incorrect password.");
                }

                var token = GenerateJwtToken(user);

                return Ok(new { Token = token, fullname = user.FullName, suspension = user.Suspension });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GenerateJwtToken(Register user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                //new Claim(ClaimTypes.Role, user.Role) // Assuming user has a Role property
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("refresh")]
        public IActionResult RefreshToken()
        {
            try
            {
                // 1. Get token from Authorization header
                var header = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
                    return Unauthorized("Invalid authorization header");

                var token = header.Substring("Bearer ".Length).Trim();

                // 2. Validate token without expiration check
                var principal = GetPrincipalFromExpiredToken(token);
                if (principal == null)
                    return Unauthorized("Invalid token");

                // 3. Extract user email from claims
                var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (emailClaim == null)
                    return Unauthorized("Invalid token claims");

                // 4. Find user in database
                var user = _context.Register.FirstOrDefault(u => u.Email == emailClaim.Value);
                if (user == null)
                    return Unauthorized("User not found");

                // 5. Generate new token
                var newToken = GenerateJwtToken(user);

                return Ok(new { token = newToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"])),
                ValidateLifetime = false // IMPORTANT: don't validate expiration
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                // Additional security: validate algorithm
                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

}
