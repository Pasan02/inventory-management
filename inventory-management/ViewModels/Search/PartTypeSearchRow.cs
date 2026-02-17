namespace inventory_management.ViewModels.Search
{
    public class PartTypeSearchRow
    {
        public int PartTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public byte[]? Image { get; set; }
    }
}
