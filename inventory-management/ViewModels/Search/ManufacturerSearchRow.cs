namespace inventory_management.ViewModels.Search
{
    public class ManufacturerSearchRow
    {
        public int ManufacturerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int Quantity { get; set; }
    }
}
