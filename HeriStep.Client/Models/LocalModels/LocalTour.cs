using SQLite;

namespace HeriStep.Client.Models.LocalModels
{
    [Table("TourCache")]
    public class LocalTour
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        [MaxLength(200)]
        public string TourName { get; set; }
        
        public string Description { get; set; }
        
        public string ImageUrl { get; set; }
        
        // Số lượt ghé thăm của Tour này
        public int Visits { get; set; }
        
        public int StallCount { get; set; }
        
        // WHERE IsActive=1 trong query offline
        public bool IsActive { get; set; } = true;
    }
}
