namespace HeriStep.Shared
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Lưu chuỗi đã băm
        public string? FullName { get; set; }
        public string Role { get; set; } = "StallOwner";
        public int? StallId { get; set; }
    }
}