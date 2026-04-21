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

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;

        public async Task OnGetAsync()
        {
            try
            {
                var allTickets = await _http.GetFromJsonAsync<List<TouristTicket>>("api/Tourists") ?? new();

                // 1. Xử lý Tìm kiếm
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    allTickets = allTickets.Where(t =>
                        (!string.IsNullOrEmpty(t.TicketCode) && t.TicketCode.ToLower().Contains(term)) ||
                        (!string.IsNullOrEmpty(t.DeviceId) && t.DeviceId.ToLower().Contains(term))
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

                // 3. LỌC NGÀY (Vì CreatedAt không có dấu ? nên gọi thẳng .Date)
                if (StartDate.HasValue)
                {
                    allTickets = allTickets.Where(t => t.CreatedAt.Date >= StartDate.Value.Date).ToList();
                }

                if (EndDate.HasValue)
                {
                    allTickets = allTickets.Where(t => t.CreatedAt.Date <= EndDate.Value.Date).ToList();
                }

                // 4. Sắp xếp: Vé mới mua nhất phải nằm trên cùng
                var sortedTickets = allTickets.OrderByDescending(t => t.CreatedAt).ToList();

                // 5. Tính toán phân trang
                int pageSize = 10;
                int totalItems = sortedTickets.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 6. Cắt danh sách lấy đúng trang hiện tại
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