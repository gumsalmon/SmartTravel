using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Subscriptions
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<Subscription> SubList { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;
        private const int PageSize = 10;

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;

            try
            {
                var allSubs = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions") ?? new();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    allSubs = allSubs.Where(s =>
                        (s.DeviceId != null && s.DeviceId.ToLower().Contains(term)) ||
                        (s.ActivationCode != null && s.ActivationCode.ToLower().Contains(term))
                    ).ToList();
                }

                if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
                {
                    TempData["Error"] = "⚠️ Ngày bắt đầu không thể lớn hơn ngày kết thúc! Hệ thống đã tự động đảo lại giúp bạn.";
                    var temp = StartDate;
                    StartDate = EndDate;
                    EndDate = temp;
                }

                if (StartDate.HasValue)
                {
                    allSubs = allSubs.Where(s => s.StartDate.HasValue && s.StartDate.Value.Date >= StartDate.Value.Date).ToList();
                }

                if (EndDate.HasValue)
                {
                    allSubs = allSubs.Where(s => s.StartDate.HasValue && s.StartDate.Value.Date <= EndDate.Value.Date).ToList();
                }

                var sortedList = allSubs.OrderByDescending(s => s.IsActive == true)
                                        .ThenByDescending(s => s.StartDate)
                                        .ToList();

                TotalPages = (int)Math.Ceiling(sortedList.Count / (double)PageSize);
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                SubList = sortedList.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi kết nối API lấy dữ liệu.";
            }
        }
    }
}