using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace inventory_management.Data.Entities
{
    [Table("items")]
    [Index(nameof(Barcode), IsUnique = true)] // STRICT duplicate prevention
    public class Item
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("barcode")]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        // --- Domain Definition ---
        [Column("part_type_id")]
        public int PartTypeId { get; set; }
        
        [ForeignKey(nameof(PartTypeId))]
        public PartType PartType { get; set; } = null!;

        [Column("vehicle_model_id")]
        public int VehicleModelId { get; set; }
        
        [ForeignKey(nameof(VehicleModelId))]
        public VehicleModel VehicleModel { get; set; } = null!;

        [Column("part_brand_id")]
        public int PartBrandId { get; set; }
        
        [ForeignKey(nameof(PartBrandId))]
        public PartBrand PartBrand { get; set; } = null!;

        [Column("country_of_origin")]
        [MaxLength(50)]
        public string CountryOfOrigin { get; set; } = string.Empty;

        // --- Metadata ---
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("image_path")]
        public string? ImagePath { get; set; } // Relative path only

        [Column("low_stock_threshold")]
        public int LowStockThreshold { get; set; } = 5;

        [Column("rack_id")]
        public int? RackId { get; set; }

        [ForeignKey(nameof(RackId))]
        public Rack? Rack { get; set; }

        // Navigation to Stock (One-to-One mostly, but kept separate for purity)
        public Stock? Stock { get; set; }
    }
}
