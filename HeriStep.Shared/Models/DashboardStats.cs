namespace HeriStep.Shared.Models
{
    public class DashboardStats
    {
        public int TotalStalls { get; set; }
        public int TotalStallOwners { get; set; }
        public int TotalTours { get; set; }
        public int ActiveDevices { get; set; }

        // 💡 BỔ SUNG 2 DÒNG NÀY VÀO ĐỂ HẾT BÁO LỖI
        public int TotalVisits { get; set; }
        public int TotalLanguages { get; set; }
    }
}