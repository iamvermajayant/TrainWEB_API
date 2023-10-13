using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models.TableSchema
{
    public class OlderTrainDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
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
