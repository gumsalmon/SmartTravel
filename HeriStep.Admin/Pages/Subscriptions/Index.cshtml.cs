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

        // 💡 CÁC BIẾN TÌM KIẾM VÀ LỌC NGÀY
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        private const int PageSize = 10;

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;

            try
            {
                var allSubs = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions") ?? new();

                // 1. TÌM KIẾM: Lọc theo DeviceId HOẶC ActivationCode
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    allSubs = allSubs.Where(s =>
                        (!string.IsNullOrEmpty(s.DeviceId) && s.DeviceId.ToLower().Contains(term)) ||
                        (!string.IsNullOrEmpty(s.ActivationCode) && s.ActivationCode.ToLower().Contains(term))
                    ).ToList();
                }

                // 2. CHẶN LỖI CHỌN NGÀY NGƯỢC
                if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
                {
                    TempData["Error"] = "⚠️ Ngày bắt đầu không thể lớn hơn ngày kết thúc! Hệ thống đã tự động đảo lại giúp bạn.";
                    var temp = StartDate;
                    StartDate = EndDate;
                    EndDate = temp;
                }

                // 3. LỌC NGÀY: Dựa vào StartDate (Ngày bắt đầu của gói)
                if (StartDate.HasValue)
                {
                    allSubs = allSubs.Where(s => s.StartDate.HasValue && s.StartDate.Value.Date >= StartDate.Value.Date).ToList();
                }

                if (EndDate.HasValue)
                {
                    allSubs = allSubs.Where(s => s.StartDate.HasValue && s.StartDate.Value.Date <= EndDate.Value.Date).ToList();
                }

                // 4. Sắp xếp dữ liệu
                var sortedList = allSubs.OrderByDescending(s => s.IsActive)
                                        .ThenByDescending(s => s.StartDate)
                                        .ToList();

                // 5. Tính toán phân trang
                TotalPages = (int)Math.Ceiling(sortedList.Count / (double)PageSize);
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 6. Cắt danh sách
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