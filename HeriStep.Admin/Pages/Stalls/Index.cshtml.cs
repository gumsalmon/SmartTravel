using HeriStep.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeriStep.Admin.Pages.Stalls
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<PointOfInterest> Stalls { get; set; } = new();

        // Khai báo biến phân trang
        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5; // Số sạp trên 1 trang

        public async Task OnGetAsync()
        {
            CurrentPage = Page > 0 ? Page : 1;

            // Tạm thời lấy toàn bộ từ API Database rồi phân trang tại Client
            // (Khi hệ thống lớn lên, ta sẽ chuyển logic phân trang này vào API)
            var allStalls = await _http.GetFromJsonAsync<List<PointOfInterest>>("api/Points") ?? new();

            // Tính toán tổng số trang
            TotalPages = (int)Math.Ceiling(allStalls.Count / (double)PageSize);

            // Cắt dữ liệu theo trang hiện tại (Skip & Take)
            Stalls = allStalls
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}