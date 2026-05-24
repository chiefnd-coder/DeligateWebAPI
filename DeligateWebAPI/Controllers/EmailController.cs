using DeligateWebAPI.Interfaces;
using DeligateWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace DeligateWebAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
      
        private readonly ILogger<EmailController> _logger;
        private readonly SmtpSettings _smtpSettings;

        public EmailController(ILogger<EmailController> logger, IOptions<SmtpSettings> smtpOptions)
        {
      
            _logger = logger;
            _smtpSettings = smtpOptions.Value;
        }

  
        [HttpPost("send")]
        public async Task<ActionResult<EmailResponse>> SendEmail([FromBody] EmailRequest request)
        {
            try
            {
                string fromName = request.FromEmail;

                using (var client = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    client.EnableSsl = true;

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(_smtpSettings.From, fromName);
                        mail.To.Add(_smtpSettings.To); 
                        mail.Subject = request.Subject ?? "No Subject";
                        mail.Body = request.Body ?? "";
                        mail.IsBodyHtml = true;

                        await client.SendMailAsync(mail);
                    }
                }

                return Ok(new EmailResponse
                {
                    Success = true,
                    Message = "Email sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email send failed");

                return StatusCode(500, new EmailResponse
                {
                    Success = false,
                    Message = "Email sending failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

}





