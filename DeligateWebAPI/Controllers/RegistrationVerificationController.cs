using DeligateWebAPI.Data;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace DeligateWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationVerificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationVerificationController> _logger;
        private readonly SmtpSettings _smtpSettings;
        public RegistrationVerificationController(ApplicationDbContext context, ILogger<RegistrationVerificationController> logger, IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _logger = logger;
            _smtpSettings = smtpOptions.Value;
        }

        // Step 1: Send  Code (sends 6-digit code to email)
        [HttpPost("send-confirmation-code")]
        public async Task<ActionResult> SendConfirmationCode([FromBody] SendConfirmationCodeRequest request)
        {


            // Send email with reset code
            await SendConfirmationCodeEmail(request.Email, request.Code, request.FullName);

            return Ok(new
            {

                message = "If the email exists, a reset code has been sent.",
                expiresIn = 15 // minutes
            });
        }





        // Helper method to send confirmation email code
        private async Task SendConfirmationCodeEmail(string toEmail, string code, string userName)
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
            <h1>Email Confirmation Code</h1>
            </div>
                    <div class='content'>
                        <p>Hello {userName},</p>
                        <p>To continue with your Lifehack Delegate registration, please enter the following verification code:</p>
                        <div class='code-box'>
                            <div class='code'>{code}</div>
                        </div>
                        <p class='warning'>
                            <strong>⏰ This code will expire in 15 minutes.</strong>
                        </p>
                        <p>If this registration was not requested by you, please ignore this message.</p>
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
                        mail.Subject = $"Your Email Confirmation Code: {code}";
                        mail.Body = emailBody;
                        mail.IsBodyHtml = true;

                        await client.SendMailAsync(mail);
                    }
                }

                _logger.LogInformation($"Confirmation code email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send confirmation code email to {toEmail}");
            }
        }

        // DTO Models
        public class SendConfirmationCodeRequest
        {
            public string Email { get; set; }
            public string Code { get; set; }

            public string FullName { get; set; }


        }

        public class VerifyCodeRequest
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }




    }
}