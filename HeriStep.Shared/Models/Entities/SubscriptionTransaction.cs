using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("SubscriptionTransactions")]
    public class SubscriptionTransaction
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("stall_id")] public int StallId { get; set; }
        [Column("amount")] public decimal Amount { get; set; }
        [Column("payment_date")] public DateTime PaymentDate { get; set; } = DateTime.Now;
        [Column("duration_days")] public int? DurationDays { get; set; }
        [Column("note")] public string? Note { get; set; }
    }
}