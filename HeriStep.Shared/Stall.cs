using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared
{
    // Class này đại diện cho bảng 'Stalls' dưới SQL
    [Table("Stalls")]
    public class Stall
    {
        public int? TourID { get; set; }
        public double? Radius { get; set; }
        public bool IsOpen { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Id { get; set; }

        [Column("name_default")] // Ánh xạ với cột name_default trong SQL
        public string Name { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Column("subscription_level")]
        public int Level { get; set; } // 0=Free, 2=Premium (Icon to)

        public string Category { get; set; } // 'Oc', 'Lau'...

        [Column("image_thumb")]
        public string ImageUrl { get; set; }
    }
}