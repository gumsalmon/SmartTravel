using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Tours
{
    public class DetailsModel : PageModel
    {
        private readonly HttpClient _http;
        public DetailsModel(HttpClient http) => _http = http;

        public Tour Tour { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Gọi API lấy chi tiết Tour kèm danh sách sạp
                var data = await _http.GetFromJsonAsync<Tour>($"api/Tours/{id}");
                if (data == null) return NotFound();

                Tour = data;
                return Page();
            }
            catch
            {
                return RedirectToPage("/Tours/Index");
            }
        }
    }
}