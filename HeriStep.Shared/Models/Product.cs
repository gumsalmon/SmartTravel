namespace HeriStep.Shared.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int StallId { get; set; }
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }

        // BỔ SUNG DÒNG NÀY ĐỂ HẾT LỖI
        public bool IsSignature { get; set; } = false;
    }
}