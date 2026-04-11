using SQLite;

namespace HeriStep.Client.Models.LocalModels
{
    [Table("StallCache")]
    public class LocalStall
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }
        
        public string ImageUrl { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public bool IsOpen { get; set; }
        public bool HasOwner { get; set; }
        
        public double RadiusMeter { get; set; }
    }
}
