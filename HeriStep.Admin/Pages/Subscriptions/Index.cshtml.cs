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

        [BindProperty]
        public Subscription NewSub { get; set; } = new();

        // 💡 CÁC BIẾN PHỤC VỤ PHÂN TRANG
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1; // Trang hiện tại, mặc định là 1
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        private const int PageSize = 10; // Số dòng trên mỗi trang

        public async Task OnGetAsync()
        {
            try
            {
                var allSubs = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions") ?? new();

                // 1. Sắp xếp dữ liệu trước khi phân trang
                var sortedList = allSubs.OrderByDescending(s => s.IsActive)
                                        .ThenByDescending(s => s.StartDate)
                                        .ToList();

                // 2. Tính toán tổng số trang
                TotalPages = (int)Math.Ceiling(sortedList.Count / (double)PageSize);

                // 3. Khống chế trang không vượt quá giới hạn
                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 4. Cắt danh sách lấy đúng phần cho trang hiện tại
                SubList = sortedList.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi kết nối API lấy dữ liệu.";
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                NewSub.StartDate = DateTime.Now;
                NewSub.ExpiryDate = DateTime.Now.AddDays(30);
                NewSub.IsActive = true;

                var response = await _http.PostAsJsonAsync("api/Subscriptions", NewSub);

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = $"✅ Đã kích hoạt {NewSub.DeviceId} thành công!";
                else
                    TempData["Error"] = "❌ Kích hoạt thất bại.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}