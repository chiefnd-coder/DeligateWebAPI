using DeligateWebAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
namespace DeligateWebAPI.Models
{
    public class DeligateTask
    {
      
        public int Id { get; set; }
        public string? TaskName { get; set; }
        public string? TaskDescription { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
    }





}
