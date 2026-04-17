using HeriStep.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages.Analytics
{
    public class IndexModel : PageModel
    {
        private readonly HeriStepDbContext _context;

        public IndexModel(HeriStepDbContext context)
        {
            _context = context;
        }

        // View Models cho biểu đồ Top POI (Sạp)
        public List<string> TopPoiLabels { get; set; } = new List<string>();
        public List<int> TopPoiCounts { get; set; } = new List<int>();

        // View Models cho biểu đồ Thời gian nghe trung bình
        public List<string> AvgListenLabels { get; set; } = new List<string>();
        public List<double> AvgListenData { get; set; } = new List<double>();

        // Tọa độ Heatmap
        public List<double[]> HeatmapData { get; set; } = new List<double[]>();

        public async Task OnGetAsync()
        {
            // 1. Top địa điểm được nghe/ghé nhiều nhất (Lấy Top 10)
            var topPois = await _context.StallVisits
                .AsNoTracking()
                .GroupBy(v => v.StallId)
                .Select(g => new
                {
                    StallId = g.Key,
                    VisitCount = g.Count()
                })
                .OrderByDescending(x => x.VisitCount)
                .Take(10)
                .Join(_context.Stalls.AsNoTracking(), 
                    visit => visit.StallId, 
                    stall => stall.Id, 
                    (visit, stall) => new 
                    { 
                        StallName = stall.Name, 
                        Count = visit.VisitCount 
                    })
                .ToListAsync();

            TopPoiLabels = topPois.Select(x => x.StallName).ToList();
            TopPoiCounts = topPois.Select(x => x.Count).ToList();

            // 2. Thời gian trung bình nghe 1 POI (Bỏ qua những record chưa tới 1s)
            var avgListen = await _context.StallVisits
                .AsNoTracking()
                .Where(v => v.ListenDurationSeconds > 0)
                .GroupBy(v => v.StallId)
                .Select(g => new
                {
                    StallId = g.Key,
                    AvgDuration = g.Average(x => x.ListenDurationSeconds)
                })
                .Join(_context.Stalls.AsNoTracking(), 
                    visit => visit.StallId, 
                    stall => stall.Id, 
                    (visit, stall) => new 
                    { 
                        StallName = stall.Name, 
                        AvgDuration = visit.AvgDuration 
                    })
                .OrderByDescending(x => x.AvgDuration)
                .ToListAsync();

            AvgListenLabels = avgListen.Select(x => x.StallName).ToList();
            AvgListenData = avgListen.Select(x => x.AvgDuration).ToList();

            // 3. Dữ liệu Heatmap 
            // Giới hạn 2000 điểm gần nhất tránh tràn RAM Frontend nếu data quá lớn
            var trajectories = await _context.TouristTrajectories
                .AsNoTracking()
                .OrderByDescending(t => t.RecordedAt)
                .Take(2000)
                .Select(t => new double[] { t.Latitude, t.Longitude, 1.0 }) // [Lat, Lng, Intensity=1.0]
                .ToListAsync();

            HeatmapData = trajectories;
        }
    }
}
