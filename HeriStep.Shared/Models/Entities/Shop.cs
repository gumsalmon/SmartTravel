namespace HeriStep.Shared.Models;

public class Shop
{
    public int Id { get; set; }
    // Thêm = string.Empty; để tránh lỗi Null
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double Distance { get; set; }
    public List<Dish> FeaturedDishes { get; set; } = new();
}