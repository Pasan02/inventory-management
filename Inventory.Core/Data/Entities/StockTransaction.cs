using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("stock_transactions")]
    public class StockTransaction
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Required]
        [Column("action_type")]
        [MaxLength(20)]
        public string ActionType { get; set; } = string.Empty; // "IN", "OUT", "ADJUST"

        [Column("quantity_change")]
        public int QuantityChange { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column("machine_name")]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        [Column("checksum_hash")]
        public string ChecksumHash { get; set; } = string.Empty; // SHA256 of the record
    }
}
