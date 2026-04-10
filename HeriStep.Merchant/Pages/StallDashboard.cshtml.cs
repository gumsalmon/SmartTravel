using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace HeriStep.Merchant.Pages
{
    [Authorize]
    public class StallDashboardModel : PageModel
    {
        private readonly HttpClient _http;

        public StallDashboardModel(HttpClient http)
        {
            _http = http;
        }

        public int CurrentStallId { get; set; }
        public Stall StallInfo { get; set; } = new Stall();

        public async Task OnGetAsync(int stallId)
        {
            CurrentStallId = stallId;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Gọi API để lấy thông tin chi tiết của sạp
                var data = await _http.GetFromJsonAsync<Stall>($"api/Stalls/{stallId}", options);

                if (data != null)
                {
                    StallInfo = data;
                }
            }
            catch
            {
                // Bắt lỗi rớt mạng hoặc API chưa có để web không bị sập
            }
        }
    }
}