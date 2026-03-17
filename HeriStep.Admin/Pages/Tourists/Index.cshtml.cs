using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Tourists
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<TouristTicket> Tourists { get; set; } = new();

        // 💡 Thuộc tính phục vụ phân trang và tìm kiếm
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;

        public async Task OnGetAsync()
        {
            try
            {
                // 1. Lấy toàn bộ danh sách vé từ API
                var allTickets = await _http.GetFromJsonAsync<List<TouristTicket>>("api/Tourists") ?? new();

                // 2. Xử lý Tìm kiếm (Nếu sếp có nhập từ khóa)
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    allTickets = allTickets.Where(t =>
                        t.TicketCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        t.DeviceId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 3. Sắp xếp: Vé mới mua nhất phải nằm trên cùng
                var sortedTickets = allTickets.OrderByDescending(t => t.CreatedAt).ToList();

                // 4. Tính toán phân trang
                int pageSize = 10; // Sếp thích 10 hay 20 dòng thì sửa ở đây
                int totalItems = sortedTickets.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Khống chế trang không vượt quá giới hạn
                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 5. Cắt danh sách lấy đúng trang hiện tại
                Tourists = sortedTickets
                    .Skip((PageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch
            {
                Tourists = new();
            }
        }
    }
}