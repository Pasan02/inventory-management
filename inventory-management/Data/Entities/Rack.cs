using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("racks")]
    public class Rack
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("location_code")]
        [MaxLength(50)]
        public string LocationCode { get; set; } = string.Empty; // e.g., A-01, B-05

        public override string ToString()
        {
            return LocationCode;
        }
    }
}
