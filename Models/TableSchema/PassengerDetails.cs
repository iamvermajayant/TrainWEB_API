using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models.TableSchema
{
    public class PassengerDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [Required]
        public int Age { get; set; }
        [Required]
        [StringLength(50)]
        public string Gender { get; set; }

        public int PNR { get; set; }
    }
}
