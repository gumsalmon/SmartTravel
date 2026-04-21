using HeriStep.Shared.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages.Stalls
{
    /// <summary>
    /// Admin chỉ được XEM danh sách sạp.
    /// Quyền Sửa / Xóa thuộc về Merchant — đã bị loại bỏ hoàn toàn.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<PointOfInterest> Stalls { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm  { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate   { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber      { get; set; } = 1;

        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        public int PageSize    { get; set; } = 10;

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;

            try
            {
                var allStalls = await _http.GetFromJsonAsync<List<PointOfInterest>>("api/Stalls") ?? new();

                // Lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    allStalls = allStalls
                        .Where(s => s.Name != null && s.Name.ToLower().Contains(term))
                        .ToList();
                }

                // Lọc theo khoảng ngày cập nhật
                if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
                    (StartDate, EndDate) = (EndDate, StartDate);

                if (StartDate.HasValue)
                    allStalls = allStalls.Where(s => s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date >= StartDate.Value.Date).ToList();

                if (EndDate.HasValue)
                    allStalls = allStalls.Where(s => s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date <= EndDate.Value.Date).ToList();

                // Phân trang
                int totalItems = allStalls.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

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
    }
}