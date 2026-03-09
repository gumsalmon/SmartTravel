using HeriStep.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Stalls
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // --- CÁC BIẾN CHỨA DỮ LIỆU HIỂN THỊ ---
        public List<PointOfInterest> Stalls { get; set; } = new();
        public List<UserDto> Owners { get; set; } = new();

        // --- CÁC BIẾN TÌM KIẾM & PHÂN TRANG ---
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5;

        // --- CÁC BIẾN HỨNG DỮ LIỆU TỪ FORM ---
        [BindProperty]
        public PointOfInterest NewStall { get; set; } = new();

        [BindProperty]
        public PointOfInterest EditStall { get; set; } = new();

        // ==========================================
        // 1. HÀM LẤY DỮ LIỆU (GET)
        // ==========================================
        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber > 0 ? PageNumber : 1;

            try
            {
                // Lấy danh sách Chủ sạp (User có role StallOwner) để đổ vào Dropdown
                Owners = await _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners") ?? new();

                // Lấy danh sách Sạp hàng
                var allStalls = await _http.GetFromJsonAsync<List<PointOfInterest>>("api/Points") ?? new();

                // Xử lý Tìm kiếm
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    allStalls = allStalls.Where(s =>
                        s.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Xử lý Phân trang
                TotalPages = (int)Math.Ceiling(allStalls.Count / (double)PageSize);
                Stalls = allStalls
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log lỗi nếu API chưa chạy
                Console.WriteLine($"Lỗi gọi API: {ex.Message}");
            }
        }

        // ==========================================
        // 2. HÀM THÊM SẠP MỚI (POST)
        // ==========================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            NewStall.UpdatedAt = DateTime.Now;
            NewStall.IsOpen = true;

            await _http.PostAsJsonAsync("api/Points", NewStall);
            return RedirectToPage(); // Tải lại trang sau khi thêm
        }

        // ==========================================
        // 3. HÀM CẬP NHẬT SẠP (PUT)
        // ==========================================
        public async Task<IActionResult> OnPostEditAsync()
        {
            EditStall.UpdatedAt = DateTime.Now;

            // Gọi API sửa dữ liệu theo ID
            await _http.PutAsJsonAsync($"api/Points/{EditStall.Id}", EditStall);
            return RedirectToPage();
        }

        // ==========================================
        // 4. HÀM XÓA SẠP (DELETE)
        // ==========================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _http.DeleteAsync($"api/Points/{id}");
            return RedirectToPage();
        }
    }
}