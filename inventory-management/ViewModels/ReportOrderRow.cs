using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace inventory_management.ViewModels
{
    public class ReportOrderRow : ObservableObject
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int Id { get; set; } // Representative OrderTracking Id
        public List<int> OrderIds { get; set; } = new();
        public int ItemId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartType { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty; // "Pending", "Ordered", "Arrived"
        public DateTime CreatedAt { get; set; }
        public DateTime? OrderedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        
        // Formatted display fields
        public string CreatedAtLocal => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        public string OrderedAtLocal => OrderedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
        public string ArrivedAtLocal => ArrivedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
    }
}
