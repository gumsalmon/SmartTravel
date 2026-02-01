using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HeriStep.Shared;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Subscriptions
{
    // ĐỔI TÊN thành SubscriptionModel để khớp với trang Subscriptions
    public class SubscriptionModel : PageModel
    {
        private readonly HttpClient _http;

        public SubscriptionModel(HttpClient http)
        {
            _http = http;
        }

        public List<Subscription> SubList { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions");
                if (response != null)
                {
                    SubList = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy dữ liệu Sub: {ex.Message}");
            }
        }
    }
}