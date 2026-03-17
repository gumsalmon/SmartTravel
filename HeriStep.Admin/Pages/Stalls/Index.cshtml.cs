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
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        public int PageSize { get; set; } = 10; // Mỗi trang 10 sạp cho sếp dễ nhìn

        [BindProperty] public PointOfInterest EditStall { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // 1. Lấy dữ liệu song song để tối ưu tốc độ
                var ownersTask = _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary");
                var stallsTask = _http.GetFromJsonAsync<List<PointOfInterest>>("api/Stalls");

                await Task.WhenAll(ownersTask, stallsTask);

                Owners = await ownersTask ?? new();
                var allStalls = await stallsTask ?? new();

                // 2. Xử lý Tìm kiếm
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    allStalls = allStalls.Where(s => s.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 3. Tính toán phân trang
                int totalItems = allStalls.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

                // Khống chế số trang hợp lệ
                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 4. Cắt danh sách (Skip/Take)
                Stalls = allStalls
                    .OrderByDescending(s => s.Id) // Mới nhất lên đầu
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

        // Handler xử lý gán chủ hàng loạt
        public async Task<IActionResult> OnPostBulkAssignAsync(List<int> SelectedStallIds, int NewOwnerId)
        {
            if (SelectedStallIds == null || !SelectedStallIds.Any()) return RedirectToPage();

            // Sếp có thể loop hoặc viết 1 API riêng. Ở đây ta loop cho đơn giản:
            foreach (var id in SelectedStallIds)
            {
                await _http.PutAsJsonAsync($"api/Stalls/assign", new { StallId = id, OwnerId = NewOwnerId });
            }

            TempData["Success"] = $"🎉 Đã cập nhật hàng loạt cho {SelectedStallIds.Count} sạp!";
            return RedirectToPage();
        }
    }
}