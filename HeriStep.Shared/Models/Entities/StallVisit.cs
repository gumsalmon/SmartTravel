using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HeriStep.Shared.Models
{
    public class StallVisit
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public int StallId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime VisitedAt { get; set; } = DateTime.Now;
        [Column("created_at_server")]
        public DateTime CreatedAtServer { get; set; } = DateTime.Now;
        public int ListenDurationSeconds { get; set; } = 0;
    }
}
