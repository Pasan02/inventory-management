using System.Windows.Controls;

namespace inventory_management.ViewModels
{
    public class ItemSearchRow
    {
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartType { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string SecretPriceCode { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; }
        public string CompatibleModelsText { get; set; } = string.Empty;
    }
}
