using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("stock")]
    public class Stock
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; } = null!;

        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
