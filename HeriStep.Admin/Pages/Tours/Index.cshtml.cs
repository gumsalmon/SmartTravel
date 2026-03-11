using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using HeriStep.Shared.Models;

namespace HeriStep.Admin.Pages.Tours // Namespace phải là .Tours
{
    // Đây mới là ToursIndexModel thật sự
    public class ToursIndexModel : PageModel
    {
        private readonly HttpClient _http;

        public ToursIndexModel(HttpClient http)
        {
            _http = http;
        }

        public List<Tour> Tours { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<List<Tour>>("api/Tours");
                if (response != null)
                {
                    Tours = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy dữ liệu Tour: {ex.Message}");
            }
        }
    }
}