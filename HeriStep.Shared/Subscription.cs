using System.ComponentModel.DataAnnotations.Schema;

public class Subscription
{
    public int Id { get; set; }
    public int StallId { get; set; } // Liên kết với sạp hàng
    public string PlanName { get; set; } = "Basic"; // Basic, Pro, Premium
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string? StallName { get; set; } // Để hiển thị lên web Admin
}