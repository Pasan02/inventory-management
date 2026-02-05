namespace inventory_management.Services
{
    public class StockOperationResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int NewQuantity { get; init; }
    }
}
