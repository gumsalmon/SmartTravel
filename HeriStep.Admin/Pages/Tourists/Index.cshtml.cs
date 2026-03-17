using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Tourists
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<TouristTicket> Tourists { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Gọi sang TouristsController vừa tạo ở Bước 1
                Tourists = await _http.GetFromJsonAsync<List<TouristTicket>>("api/Tourists") ?? new();
            }
            catch
            {
                Tourists = new();
            }
        }
    }
}