using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("part_types")]
    public class PartType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., Compressor, Condenser

        [Column("image_path")]
        [MaxLength(260)]
        public string? ImagePath { get; set; }

        [Column("image")]
        public byte[]? Image { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
