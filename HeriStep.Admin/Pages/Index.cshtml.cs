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
                // 💡 ĐÃ SỬA: Đổi từ "api/Dashboard/stats" thành "api/Stats" cho khớp với Controller của bạn
                Stats = await _http.GetFromJsonAsync<DashboardStats>("api/Stats") ?? new DashboardStats();
            }
            catch
            {
                Stats = new DashboardStats();
            }
        }
    }
}