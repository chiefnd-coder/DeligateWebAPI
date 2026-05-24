using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using DeligateWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DeligateWebAPI.Models
{
    public class UserTracker
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Country { get; set; }
        public string?  Action { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Errormessage { get; set; }


    }
}
