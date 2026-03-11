using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Subscriptions
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<Subscription> SubList { get; set; } = new();
        [BindProperty] public Subscription NewSub { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                SubList = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions") ?? new();
            }
            catch
            {
                TempData["Error"] = "❌ Không thể kết nối tới server API Subscriptions.";
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Tự động tính ngày hết hạn là 30 ngày kể từ lúc kích hoạt
            NewSub.StartDate = DateTime.Now;
            NewSub.ExpiryDate = DateTime.Now.AddDays(30);
            NewSub.IsActive = true;

            var response = await _http.PostAsJsonAsync("api/Subscriptions", NewSub);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "✅ Kích hoạt gói cước thiết bị thành công!";
                return RedirectToPage();
            }
            TempData["Error"] = "❌ Lỗi: Mã kích hoạt đã tồn tại hoặc DeviceID sai!";
            return RedirectToPage();
        }
    }
}