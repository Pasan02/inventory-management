using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("vehicle_models")]
    public class VehicleModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("manufacturer_id")]
        public int VehicleManufacturerId { get; set; }

        [ForeignKey(nameof(VehicleManufacturerId))]
        public VehicleManufacturer Manufacturer { get; set; } = null!;

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., Corolla 2010-2015

        [Column("year_range")]
        [MaxLength(50)]
        public string? YearRange { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
