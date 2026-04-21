using HeriStep.Shared.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages.Stalls
{
    /// <summary>
    /// Trang xem chi tiết 1 Sạp hàng (View-only).
    /// Dữ liệu lấy từ GET /api/Stalls/{id} qua HttpClient.
    /// </summary>
    public class DetailModel : PageModel
    {
        private readonly HttpClient _http;
        public DetailModel(HttpClient http) => _http = http;

        public PointOfInterest? Stall { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0) return RedirectToPage("./Index");

            try
            {
                Stall = await _http.GetFromJsonAsync<PointOfInterest>($"api/Stalls/{id}");
                if (Stall == null) return NotFound();
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Không thể tải thông tin sạp #{id}: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }
    }
}
