using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.DTO
{
    public class TrainDetailsModelToDB
    {
        [Required]
        [StringLength(100)]
        public string TrainName { get; set; }
        [Required]
        public int TrainId { get; set; }
        [Required]
        [StringLength(100)]
        public string Origin { get; set; }
        [Required]
        [StringLength(100)]
        public string Destination { get; set; }
        [Required]
        public DateTime Departure { get; set; }
        [Required]
        public DateTime Arrival { get; set; }
        [Required]
        public int SeatCapacity { get; set; }
        [Required]
        public int SeatRate { get; set; }
    }
}
