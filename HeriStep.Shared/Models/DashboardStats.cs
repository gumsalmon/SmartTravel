namespace HeriStep.Shared.Models
{
    public class DashboardStats
    {
        // Đếm từ bảng Stalls
        public int TotalStalls { get; set; }

        // Đếm từ bảng Users (với Role là StallOwner)
        public int TotalStallOwners { get; set; }

        // Đếm từ bảng Tours (các lộ trình đang IsActive = true)
        public int TotalTours { get; set; }

        // Đếm từ bảng Subscriptions (các gói đang IsActive = true)
        public int ActiveDevices { get; set; }
    }
}