namespace inventory_management.ViewModels.Search
{
    public class ModelSearchRow
    {
        public int ModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string YearRange { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int Quantity { get; set; }
    }
}
