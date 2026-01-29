using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    // Giả lập database từ SSMS
    private List<Shop> _allShops = new List<Shop>();

    [HttpGet("nearby")]
    public IActionResult GetNearbyShops(double userLat, double userLon)
    {
        // Logic: Tính khoảng cách và lấy các shop trong bán kính 2km
        var nearby = _allShops.Select(s => {
            s.Distance = CalculateDistance(userLat, userLon, s.Latitude, s.Longitude);
            return s;
        })
        .Where(s => s.Distance <= 2.0) // Bán kính 2km
        .OrderBy(s => s.Distance)
        .ToList();

        return Ok(nearby);
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Công thức Haversine để tính khoảng cách trên mặt cầu
        var R = 6371; // Bán kính Trái Đất (km)
        var dLat = (lat2 - lat1) * (Math.PI / 180);
        var dLon = (lon2 - lon1) * (Math.PI / 180);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}