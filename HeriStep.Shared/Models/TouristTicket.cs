using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared.Models
{
    [Table("TouristTickets")]
    public class TouristTicket
    {
        [Key]
        public int Id { get; set; }
        [Column("ticket_code")]
        public string TicketCode { get; set; }
        [Column("device_id")]
        public string DeviceId { get; set; }
        [Column("package_id")]
        public int PackageId { get; set; }
        [Column("amount_paid")]
        public decimal AmountPaid { get; set; }
        [Column("payment_method")]
        public string PaymentMethod { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        // Biến ảo để hứng tên gói vé hiển thị lên Web
        [NotMapped]
        [JsonPropertyName("packageName")]
        public string? PackageName { get; set; }
    }
}