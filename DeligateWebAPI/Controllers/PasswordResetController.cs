
using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordResetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PasswordResetController> _logger;
        private readonly SmtpSettings _smtpSettings;
        public PasswordResetController(ApplicationDbContext context, ILogger<PasswordResetController> logger, IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _logger = logger;
            _smtpSettings = smtpOptions.Value;
        }

        // Step 1: Request Password Reset Code (sends 6-digit code to email)
        //[HttpPost("request-reset-code")]
        //public async Task<ActionResult> RequestResetCode([FromBody] ResetCodeRequest request)
        //{
        //    var user = await _context.Register.FirstOrDefaultAsync(r => r.Email == request.Email);

        //    if (user == null)
        //    {
        //        // Return success even if user doesn't exist (security best practice)
        //        return Ok(new { message = "If the email exists, a reset code has been sent." });
        //    }


        //    // Generate 6-digit reset code
        //    //var resetCode = new Random().Next(100000, 999999).ToString();
        //    var resetCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        //    var resetCodeExpiry = DateTime.UtcNow.AddMinutes(15); // Code valid for 15 minutes

        //    // Store code in database
        //    user.ResetCode = resetCode;
        //    user.ResetCodeExpiry = resetCodeExpiry;

        //    await _context.SaveChangesAsync();

        //    // Send email with reset code
        //    await SendResetCodeEmail(user.Email, user.FullName ?? "User", resetCode);

        //    return Ok(new
        //    {
        //        message = "If the email exists, a reset code has been sent.",
        //        expiresIn = 15 // minutes
        //    });
        //}

        [HttpPost("request-reset-code")]
        public async Task<ActionResult> RequestResetCode([FromBody] ResetCodeRequest request)
        {
            var user = await _context.Register
                .FirstOrDefaultAsync(r => r.Email == request.Email);

            if (user == null)
                return Ok(new { message = "If the email exists, a reset code has been sent." });

            var now = DateTime.UtcNow;

            // 2 minute cooldown between requests
            if (user.LastResetCodeRequestedAt.HasValue &&
                (now - user.LastResetCodeRequestedAt.Value).TotalMinutes < 2)
            {
                var secondsLeft = 120 - (int)(now - user.LastResetCodeRequestedAt.Value).TotalSeconds;
                return BadRequest(new { message = $"Please wait {secondsLeft} seconds before requesting another code." });
            }

            // Max 3 requests per hour
            if (user.ResetCodeRequestWindowStart.HasValue &&
                (now - user.ResetCodeRequestWindowStart.Value).TotalHours < 1)
            {
                if (user.ResetCodeRequestCount >= 3)
                    return BadRequest(new { message = "Too many reset attempts. Please try again in an hour." });

                user.ResetCodeRequestCount++;
            }
            else
            {
                user.ResetCodeRequestWindowStart = now;
                user.ResetCodeRequestCount = 1;
            }

            // Max 3 requests per day
            if (user.DailyResetCodeWindowStart.HasValue &&
                (now - user.DailyResetCodeWindowStart.Value).TotalHours < 24)
            {
                if (user.DailyResetCodeRequestCount >= 3)
                    return BadRequest(new { message = "Daily reset limit reached. Please try again tomorrow." });

                user.DailyResetCodeRequestCount++;
            }
            else
            {
                user.DailyResetCodeWindowStart = now;
                user.DailyResetCodeRequestCount = 1;
            }

            // Generate and store reset code
            var resetCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.ResetCode = resetCode;
            user.ResetCodeExpiry = now.AddMinutes(15);
            user.LastResetCodeRequestedAt = now;

            await _context.SaveChangesAsync();

            await SendResetCodeEmail(user.Email, user.FullName ?? "User", resetCode);

            return Ok(new
            {
                message = "If the email exists, a reset code has been sent.",
                expiresIn = 15
            });
        }

        // Step 2: Verify Reset Code
        [HttpPost("verify-reset-code")]
        public async Task<ActionResult> VerifyResetCode([FromBody] VerifyCodeRequest request)
        {
            var user = await _context.Register
                .FirstOrDefaultAsync(r => r.Email == request.Email);

            if (user == null)
                return BadRequest(new { message = "Invalid reset code." });

            if (user.ResetCodeFailedAttempts >= 5)
                return BadRequest(new { message = "Too many failed attempts. Request a new code." });

            // ✅ Constant time comparison
            bool codeMatches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.ResetCode ?? ""),
                Encoding.UTF8.GetBytes(request.Code ?? "")
            );

            // ✅ Use codeMatches here, NOT user.ResetCode != request.Code
            if (!codeMatches || user.ResetCodeExpiry == null || user.ResetCodeExpiry < DateTime.UtcNow)
            {
                user.ResetCodeFailedAttempts++;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "Invalid or expired reset code." });
            }

            // Success - clear code immediately
            user.ResetCodeFailedAttempts = 0;
            user.ResetCode = null;
            user.ResetCodeExpiry = null;

            var resetToken = Guid.NewGuid().ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Code verified successfully.",
                resetToken = resetToken
            });
        }

        // Step 3: Reset Password with Token
        [HttpPost("reset-password-with-token")]
        public async Task<ActionResult> ResetPasswordWithToken([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Register
                .FirstOrDefaultAsync(r => r.ResetToken == request.ResetToken);

            if (user == null)
            {
                return BadRequest(new { message = "Invalid or expired reset session." });
            }

            // Check if token has expired
            if (user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Reset session has expired. Please request a new code." });
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Clear all reset-related fields
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully.", password = user.Password });
        }

        // Helper method to send reset code email
        private async Task SendResetCodeEmail(string toEmail, string userName, string resetCode)
        {
            try
            {
                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ed1774; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 5px; margin-top: 20px; }}
        .code-box {{ background-color: #fff; border: 2px dashed #ed1774; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px; }}
        .code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #ed1774; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ color: #4CAF50; margin-top: 20px; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
      
            <img src='https://www.lifehackdelegate.com/images/logo.png' 
                 alt='Lifehack Delegate' 
                 style='max-width:180px; height:auto; margin-bottom:0px;' />
            <h1>Password Reset Code</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password. Use the code below to continue:</p>
            <div class='code-box'>
                <div class='code'>{resetCode}</div>
            </div>
            <p class='warning'>
                <strong>⏰ This code will expire in 15 minutes.</strong>
            </p>
            <p>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
            <p style='margin-top: 30px; font-size: 12px; color: #666;'>
                For security reasons, never share this code with anyone.
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply.</p>
            <p>&copy; {DateTime.UtcNow.Year} Lifehack Delegate. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                using (var client = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    client.EnableSsl = true;

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(_smtpSettings.From, "Lifehack Delegate");
                        mail.To.Add(toEmail);
                        mail.Subject = $"Your Password Reset Code: {resetCode}";
                        mail.Body = emailBody;
                        mail.IsBodyHtml = true;

                        await client.SendMailAsync(mail);
                    }
                }

                _logger.LogInformation($"Reset code email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send reset code email to {toEmail}");
            }
        }

        // DTO Models
        public class ResetCodeRequest
        {
            public string Email { get; set; }
        }

        public class VerifyCodeRequest
        {
            public string Email { get; set; }
            public string Code { get; set; }



        }

        public class ResetPasswordRequest
        {
            public string ResetToken { get; set; }
            public string NewPassword { get; set; }
        }



    }
}