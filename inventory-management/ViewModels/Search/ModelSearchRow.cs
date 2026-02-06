namespace inventory_management.ViewModels.Search
{
    public class ModelSearchRow
    {
        public int ModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string YearRange { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int Quantity { get; set; }
        public string Brands { get; set; } = string.Empty;
        public string Racks { get; set; } = string.Empty;
        public string CountriesOfOrigin { get; set; } = string.Empty;
        public string? PartTypeImagePath { get; set; }
        public string? ManufacturerLogoPath { get; set; }
    }
}
