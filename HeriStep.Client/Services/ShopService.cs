using System.Net.Http.Json;
using HeriStep.Shared.Models; // Cần thiết để nhận diện lớp Shop

namespace HeriStep.Client.Services;

public class ShopService
{
    private readonly HttpClient _httpClient;

    // DI sẽ tự động truyền HttpClient từ MauiProgram vào đây
    public ShopService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Shop>> GetNearbyShopsAsync(double lat, double lon)
    {
        try
        {
            // Vì đã có BaseAddress trong MauiProgram, bạn chỉ cần ghi phần còn lại của URL
            var response = await _httpClient.GetFromJsonAsync<List<Shop>>(
                $"api/shop/nearby?userLat={lat}&userLon={lon}");

            return response ?? new List<Shop>();
        }
        catch (Exception ex)
        {
            // Log lỗi nếu cần thiết (phù hợp với kỹ năng debug của sinh viên IT)
            System.Diagnostics.Debug.WriteLine($"Lỗi gọi API: {ex.Message}");
            return new List<Shop>();
        }
    }
}