using System.ComponentModel.DataAnnotations;
namespace HeriStep.Shared.Models
{
    public class StallVisit
    {
        [Key] public int Id { get; set; }
        public int StallId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime VisitedAt { get; set; } = DateTime.Now;
    }
}