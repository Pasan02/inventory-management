using System;

namespace inventory_management.ViewModels
{
    public class ReportTransactionRow
    {
        public DateTime Timestamp { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public string MachineName { get; set; } = string.Empty;
    }
}
