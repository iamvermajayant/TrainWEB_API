using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace WebApi.Models.TableSchema
{
    public class BookingHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]

        public int PNR { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }
        [Required]

        public int ticketCount { get; set; }

        public int? TrainId { get; set; }
        public int? UserId { get; set; }

        [ForeignKey("TrainId")]
        public virtual TrainDetails? TrainDetails { get; set; }
        [ForeignKey("UserId")]
        public virtual UserProfileDetails? UserProfileDetails { get; set; }
    }
}
