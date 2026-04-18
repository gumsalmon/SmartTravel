using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages.Analytics
{
    // DTO nội bộ để deserialize kết quả từ API avg-listen-time
    internal class AvgListenItem
    {
        public string StallName  { get; set; } = string.Empty;
        public double AvgSeconds { get; set; }
        public int    VisitCount { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;

        public IndexModel(HttpClient http) => _http = http;

        // Dữ liệu cho biểu đồ Bar (Top 10 sạp theo số lượt nghe)
        public List<string> TopPoiLabels { get; set; } = new();
        public List<int>    TopPoiCounts { get; set; } = new();

        // Dữ liệu cho biểu đồ Donut (thời gian nghe trung bình / sạp)
        public List<string> AvgListenLabels { get; set; } = new();
        public List<double> AvgListenData   { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var avgData = await _http.GetFromJsonAsync<List<AvgListenItem>>(
                    "http://localhost:5297/api/analytics/avg-listen-time")
                    ?? new List<AvgListenItem>();

                // Biểu đồ Bar: Top 10 sạp theo số lượt (VisitCount)
                var topOrdered = avgData.OrderByDescending(x => x.VisitCount).Take(10).ToList();
                TopPoiLabels = topOrdered.Select(x => x.StallName).ToList();
                TopPoiCounts = topOrdered.Select(x => x.VisitCount).ToList();

                // Biểu đồ Donut: Tất cả sạp, sắp xếp theo thời gian nghe trung bình giảm dần
                var avgOrdered = avgData.OrderByDescending(x => x.AvgSeconds).ToList();
                AvgListenLabels = avgOrdered.Select(x => x.StallName).ToList();
                AvgListenData   = avgOrdered.Select(x => Math.Round(x.AvgSeconds, 1)).ToList();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Analytics] avg-listen-time error: {ex.Message}");
            }
        }
    }
}
