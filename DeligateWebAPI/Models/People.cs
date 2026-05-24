using System.ComponentModel.DataAnnotations;

namespace DeligateWebAPI.Models
{
    public class People
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public int DesignationId { get; set; }
        public string? UserEmail { get; set; }
        
    }
}
