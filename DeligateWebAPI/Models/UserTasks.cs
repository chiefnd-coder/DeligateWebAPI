

namespace DeligateWebAPI.Models
{
    public class UserTasks
    {
        public int Id { get; set; }
        public string? TaskName { get; set; }
        public int? RemoteTaskId { get; set; }
        public string? TaskDetail { get; set; }
        public string? CategoryName { get; set; }
        public string? DeligatedTo { get; set; }
        public string? DelegateStatus { get; set; }
        public string? DelegatorEmail { get; set; }
        public string? DelegatorName { get; set; }
        public string? DelegatorPhoneNumber { get; set; }
        public string? VendorEmail { get; set; }
        public string? VendorName { get; set; }
        public string? VendorPhoneNumber { get; set; }
        public string? NonVendorEmail { get; set; }
        public string? NonVendorName { get; set; }
        public string? NonVendorPhoneNumber { get; set; }
        public int? Ratings { get; set; }
        public int? MessageStatus { get; set; }
        public int? VendorTaskId { get; set; }
        public string? Status { get; set; }
        public string? StatusColor { get; set; }
        public string? StatusSorting { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? Date { get; set; } // Store as ISO 8601 format 'YYYY-MM-DD'
        public string? StartTime { get; set; } // Store as 'HH:MM:SS'
        public string? EndTime { get; set; }   // Store as 'HH:MM:SS'
    }
}
