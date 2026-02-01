using HeriStep.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // Khai báo các thuộc tính để lưu dữ liệu từ API
        public List<PointOfInterest> Stalls { get; set; } = new();
        public List<Tour> Tours { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Gọi API lấy dữ liệu thực tế từ Database
                var stallsTask = _http.GetFromJsonAsync<List<PointOfInterest>>("api/Points");
                var toursTask = _http.GetFromJsonAsync<List<Tour>>("api/Tours");
                var statsTask = _http.GetFromJsonAsync<DashboardStats>("api/Stats");

                // Đợi tất cả dữ liệu trả về
                Stalls = await stallsTask ?? new();
                Tours = await toursTask ?? new();
                Stats = await statsTask ?? new();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu API không phản hồi
                Console.WriteLine($"Lỗi tải dữ liệu Dashboard: {ex.Message}");
            }
        }
    }
}