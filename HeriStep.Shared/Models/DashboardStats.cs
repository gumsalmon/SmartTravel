using System;
using System.Collections.Generic;

namespace HeriStep.Shared.Models
{
    public class DashboardStats
    {
        public int TotalStalls { get; set; }
        public int TotalStallOwners { get; set; }
        public int TotalTours { get; set; }
        public int ActiveDevices { get; set; }
        public int TotalVisits { get; set; }
        public int TotalLanguages { get; set; }
        public int OpenStalls { get; set; }
        public int ClosedStalls { get; set; }

        // Biểu đồ chính
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartData { get; set; } = new();

        // Biểu đồ Top sạp
        public List<string> TopStallNames { get; set; } = new();
        public List<int> TopStallVisits { get; set; } = new();

        // Biểu đồ Doanh thu (QUAN TRỌNG: Cần đúng tên này)
        public List<string> RevenueLabels { get; set; } = new();
        public List<double> RevenueData { get; set; } = new();
    }
}