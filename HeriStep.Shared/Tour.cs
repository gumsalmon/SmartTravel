using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên Tour không được để trống")]
        public string TourName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = "default-tour.jpg";

        public bool IsActive { get; set; } = true;

        // Thuộc tính phụ để đếm số sạp hàng trong Tour
        [NotMapped]
        public int StallCount { get; set; }
    }
}