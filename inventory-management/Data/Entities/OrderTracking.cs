using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("order_tracking")]
    public class OrderTracking
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("status")]
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Ordered", "Arrived"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("ordered_at")]
        public DateTime? OrderedAt { get; set; }

        [Column("arrived_at")]
        public DateTime? ArrivedAt { get; set; }
    }
}
