using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("vehicle_manufacturers")]
    public class VehicleManufacturer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., Toyota, Ford

        [Column("logo_path")]
        [MaxLength(260)]
        public string? LogoPath { get; set; }

        [Column("logo")]
        public byte[]? Logo { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
