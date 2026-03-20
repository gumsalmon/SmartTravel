using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Stalls
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // --- DỮ LIỆU ---
        public List<PointOfInterest> Stalls { get; set; } = new();
        public List<UserDto> Owners { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        // 💡 Thay FilterDate bằng Từ ngày - Đến ngày
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        public int PageSize { get; set; } = 10;

        [BindProperty] public PointOfInterest EditStall { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;

            try
            {
                var ownersTask = _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary");
                var stallsTask = _http.GetFromJsonAsync<List<PointOfInterest>>("api/Stalls");

                await Task.WhenAll(ownersTask, stallsTask);

                Owners = await ownersTask ?? new();
                var allStalls = await stallsTask ?? new();

                // 1. Lọc theo Tên Sạp HOẶC Tên Chủ Sạp HOẶC Số điện thoại (Username)
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();

                    // Tìm ra danh sách ID của các chủ sạp có Tên hoặc SĐT khớp với từ khóa
                    var matchingOwnerIds = Owners
                        .Where(o => (!string.IsNullOrEmpty(o.Username) && o.Username.Contains(SearchTerm)) ||
                                    (!string.IsNullOrEmpty(o.FullName) && o.FullName.ToLower().Contains(searchTermLower)))
                        .Select(o => o.Id)
                        .ToList();

                    allStalls = allStalls.Where(s =>
                        (s.Name != null && s.Name.ToLower().Contains(searchTermLower)) ||
                        (s.OwnerId.HasValue && matchingOwnerIds.Contains(s.OwnerId.Value))
                    ).ToList();
                }

                // ... (khúc lọc theo Tên và SĐT giữ nguyên) ...

                // 💡 CHẶN LỖI CHỌN NGÀY NGƯỢC ĐỜI (END < START)
                if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
                {
                    TempData["Error"] = "⚠️ Ngày bắt đầu không thể lớn hơn ngày kết thúc! Hệ thống đã tự động đảo lại giúp bạn.";

                    // Tự động hoán đổi 2 ngày lại cho đúng logic (Pro UX)
                    var temp = StartDate;
                    StartDate = EndDate;
                    EndDate = temp;
                }

                // 2. Lọc theo Khoảng thời gian (Từ ngày -> Đến ngày)
                if (StartDate.HasValue)
                {
                    allStalls = allStalls.Where(s => s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date >= StartDate.Value.Date).ToList();
                }

                if (EndDate.HasValue)
                {
                    allStalls = allStalls.Where(s => s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date <= EndDate.Value.Date).ToList();
                }

                // 3. Tính toán Phân trang
                int totalItems = allStalls.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 4. Cắt danh sách
                Stalls = allStalls
                    .OrderByDescending(s => s.Id)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi kết nối API: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            ModelState.Remove("EditStall.ImageUrl");
            if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }

            try
            {
                var response = await _http.PutAsJsonAsync($"api/Stalls/{EditStall.Id}", EditStall);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"✅ Cập nhật sạp {EditStall.Name} thành công!";
                    return RedirectToPage();
                }
            }
            catch { TempData["Error"] = "❌ Lỗi hệ thống khi cập nhật."; }

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/Stalls/{id}");
            if (response.IsSuccessStatusCode) TempData["Success"] = "🗑️ Đã xóa sạp hàng.";
            else TempData["Error"] = "❌ Lỗi xóa sạp.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkAssignAsync(List<int> SelectedStallIds, int NewOwnerId)
        {
            if (SelectedStallIds == null || !SelectedStallIds.Any()) return RedirectToPage();

            foreach (var id in SelectedStallIds)
            {
                await _http.PutAsJsonAsync($"api/Stalls/assign", new { StallId = id, OwnerId = NewOwnerId });
            }

            TempData["Success"] = $"🎉 Đã cập nhật hàng loạt cho {SelectedStallIds.Count} sạp!";
            return RedirectToPage();
        }
    }
}