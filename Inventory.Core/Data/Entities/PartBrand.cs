using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("part_brands")]
    public class PartBrand
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., Denso, Bosch

        public override string ToString()
        {
            return Name;
        }
    }
}
