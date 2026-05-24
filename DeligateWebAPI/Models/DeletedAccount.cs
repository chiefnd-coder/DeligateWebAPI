using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using DeligateWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Models
{
    public class DeletedAccount
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? UserUniqueId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public string?  Password { get; set; }
        public string? DeviceToken { get; set; }
        public string? Suspension { get; set; }
        public bool? IsTokenActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public string? ResetCode { get; set; }
        public DateTime? ResetCodeExpiry { get; set; }
    
    }
}
