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

        // --- DỮ LIỆU HIỂN THỊ ---
        public List<PointOfInterest> Stalls { get; set; } = new();
        public List<UserDto> Owners { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5;

        [BindProperty] public PointOfInterest EditStall { get; set; } = new();

        // ==========================================
        // 1. LẤY DỮ LIỆU (GET)
        // ==========================================
        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber > 0 ? PageNumber : 1;
            try
            {
                // Lấy danh sách Chủ sạp và Sạp hàng cùng lúc
                var ownersTask = _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary");
                var stallsTask = _http.GetFromJsonAsync<List<PointOfInterest>>("api/Points");

                await Task.WhenAll(ownersTask, stallsTask);

                Owners = await ownersTask ?? new();
                var allStalls = await stallsTask ?? new();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    allStalls = allStalls.Where(s => s.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                TotalPages = (int)Math.Ceiling(allStalls.Count / (double)PageSize);
                Stalls = allStalls.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API Dashboard."; }
        }

        // ==========================================
        // 2. HÀM CẬP NHẬT SẠP (PUT)
        // ==========================================
        public async Task<IActionResult> OnPostEditAsync()
        {
            // BƯỚC 1: Xóa sạch validation cũ
            ModelState.Clear();

            // BƯỚC 2: Chỉ ép buộc kiểm tra dữ liệu của EditStall
            if (!TryValidateModel(EditStall, nameof(EditStall)))
            {
                TempData["Error"] = "❌ Thông tin chỉnh sửa không hợp lệ!";
                await OnGetAsync();
                return Page();
            }

            try
            {
                var response = await _http.PutAsJsonAsync($"api/Points/{EditStall.Id}", EditStall);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"✅ Cập nhật sạp #{EditStall.Id} thành công!";
                    return RedirectToPage();
                }
                TempData["Error"] = "❌ Cập nhật thất bại.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối khi cập nhật."; }

            await OnGetAsync();
            return Page();
        }

        // ==========================================
        // 3. HÀM XÓA SẠP (DELETE)
        // ==========================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/Points/{id}");
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "🗑️ Đã xóa sạp hàng thành công!";
            else
                TempData["Error"] = "❌ Không thể xóa sạp hàng này.";

            return RedirectToPage();
        }
    }
}