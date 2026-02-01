public class Product
{
    public int Id { get; set; }
    public int StallId { get; set; }
    public decimal BasePrice { get; set; }
    public string ImageUrl { get; set; } = "";
    public string ProductName { get; set; } = ""; // Lấy từ bảng ProductTranslations
}