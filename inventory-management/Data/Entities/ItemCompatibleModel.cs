using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("item_compatible_models")]
    public class ItemCompatibleModel
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Column("manufacturer")]
        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [Column("model")]
        [MaxLength(100)]
        public string? Model { get; set; }

        [Column("year_range")]
        [MaxLength(50)]
        public string? YearRange { get; set; }

        [Column("brand")]
        [MaxLength(100)]
        public string? Brand { get; set; }

        [Column("country_of_origin")]
        [MaxLength(100)]
        public string? CountryOfOrigin { get; set; }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            var namePart = "";
            if (!string.IsNullOrWhiteSpace(Manufacturer))
                namePart += Manufacturer;
            if (!string.IsNullOrWhiteSpace(Model))
                namePart += (namePart.Length > 0 ? " " : "") + Model;
            
            if (!string.IsNullOrWhiteSpace(YearRange))
                namePart += (namePart.Length > 0 ? " " : "") + $"({YearRange})";
            
            if (!string.IsNullOrWhiteSpace(namePart))
                parts.Add(namePart);

            var detailPart = "";
            if (!string.IsNullOrWhiteSpace(Brand))
                detailPart += Brand;
            if (!string.IsNullOrWhiteSpace(CountryOfOrigin))
                detailPart += (detailPart.Length > 0 ? " - " : "") + CountryOfOrigin;
            
            if (!string.IsNullOrWhiteSpace(detailPart))
                parts.Add($"[{detailPart}]");

            return parts.Count > 0 ? string.Join(" ", parts) : "(Empty compatibility)";
        }
    }
}

