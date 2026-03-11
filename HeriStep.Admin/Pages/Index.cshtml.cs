using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // 💡 Tối ưu: Chỉ cần lưu 1 Object duy nhất chứa các con số tổng
        public DashboardStats Stats { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Gọi ĐÚNG cái đường dẫn API mà bạn vừa viết bên DashboardController
                Stats = await _http.GetFromJsonAsync<DashboardStats>("api/Dashboard/stats") ?? new();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu API không phản hồi hoặc chưa chạy
                Console.WriteLine($"❌ Lỗi tải dữ liệu Dashboard: {ex.Message}");
            }
        }
    }
}