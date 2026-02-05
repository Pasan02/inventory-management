using System.Collections.ObjectModel;
using System.Windows;
using inventory_management.Data;
using inventory_management.Data.Entities;
using inventory_management.Services;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace inventory_management.ViewModels
{
    public partial class ItemCreationViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IBarcodeService _barcodeService;
        private readonly IDatabaseAvailabilityService _availabilityService;

        // --- Form Properties ---
        
        private string _selectedBarcode = string.Empty;
        public string SelectedBarcode
        {
            get => _selectedBarcode;
            set => SetProperty(ref _selectedBarcode, value);
        }

        private ImageSource? _barcodeImage;
        public ImageSource? BarcodeImage
        {
            get => _barcodeImage;
            set => SetProperty(ref _barcodeImage, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _lowStockThreshold = 5;
        public int LowStockThreshold
        {
            get => _lowStockThreshold;
            set => SetProperty(ref _lowStockThreshold, value);
        }

        private string _countryOfOrigin = string.Empty;
        public string CountryOfOrigin
        {
            get => _countryOfOrigin;
            set => SetProperty(ref _countryOfOrigin, value);
        }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        // --- Dropdowns ---

        public ObservableCollection<PartType> PartTypes { get; } = new();

        private PartType? _selectedPartType;
        public PartType? SelectedPartType
        {
            get => _selectedPartType;
            set => SetProperty(ref _selectedPartType, value);
        }


        public ObservableCollection<VehicleManufacturer> Manufacturers { get; } = new();

        private VehicleManufacturer? _selectedManufacturer;
        public VehicleManufacturer? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set 
            {
                if (SetProperty(ref _selectedManufacturer, value))
                {
                    OnSelectedManufacturerChanged(value);
                }
            }
        }

        public ObservableCollection<VehicleModel> Models { get; } = new();
        
        private VehicleModel? _selectedModel;
        public VehicleModel? SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

        public ObservableCollection<PartBrand> Brands { get; } = new();
        
        private PartBrand? _selectedBrand;
        public PartBrand? SelectedBrand
        {
            get => _selectedBrand;
            set => SetProperty(ref _selectedBrand, value);
        }
        
        public ObservableCollection<Rack> Racks { get; } = new();
        
        private Rack? _selectedRack;
        public Rack? SelectedRack
        {
            get => _selectedRack;
            set => SetProperty(ref _selectedRack, value);
        }

        // --- UI State ---
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ItemCreationViewModel(InventoryDbContext context, IBarcodeService barcodeService, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _barcodeService = barcodeService;
            _availabilityService = availabilityService;
            
            LoadReferenceData();
        }

        private async void LoadReferenceData()
        {
            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                // Load lookups
                var types = await _context.PartTypes.ToListAsync();
                foreach (var t in types) PartTypes.Add(t);

                var brands = await _context.PartBrands.ToListAsync();
                foreach (var b in brands) Brands.Add(b);

                var manufacturers = await _context.Manufacturers.ToListAsync();
                foreach (var m in manufacturers) Manufacturers.Add(m);

                var racks = await _context.Racks.ToListAsync();
                foreach (var r in racks) Racks.Add(r);

            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }
        }

        private void OnSelectedManufacturerChanged(VehicleManufacturer? value)
        {
            Models.Clear();
            SelectedModel = null;
            if (value != null)
            {
                var models = _context.Models.Where(m => m.VehicleManufacturerId == value.Id).ToList();
                foreach (var m in models) Models.Add(m);
            }
        }

        [RelayCommand]
        private async Task SaveItem()
        {
            if (!ValidateForm()) return;

            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                StatusMessage = "Saving...";

                // Create Item
                var newItem = new Item
                {
                    PartTypeId = SelectedPartType!.Id,
                    PartBrandId = SelectedBrand!.Id,
                    VehicleModelId = SelectedModel!.Id,
                    CountryOfOrigin = CountryOfOrigin,
                    Description = Description,
                    LowStockThreshold = LowStockThreshold,
                    RackId = SelectedRack?.Id,
                    Barcode = "TEMP-" + Guid.NewGuid().ToString().Substring(0,8) 
                };

                _context.Items.Add(newItem);
                await _context.SaveChangesAsync();

                // Generate real barcode
                newItem.Barcode = _barcodeService.GenerateBarcodeString(newItem.Id);
                
                var initialStock = new Stock
                {
                    ItemId = newItem.Id,
                    Quantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.Stocks.Add(initialStock);

                await _context.SaveChangesAsync();

                // Generate Image for UI
                SelectedBarcode = newItem.Barcode;
                var bytes = _barcodeService.GenerateBarcodeImage(newItem.Barcode);
                BarcodeImage = BytesToImage(bytes);

                StatusMessage = $"Item Saved! Barcode: {newItem.Barcode}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
            }
        }

        private ImageSource? BytesToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(bytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private bool ValidateForm()
        {
            if (SelectedPartType == null) { StatusMessage = "Part Type required"; return false; }
            if (SelectedBrand == null) { StatusMessage = "Brand required"; return false; }
            if (SelectedManufacturer == null) { StatusMessage = "Manufacturer required"; return false; }
            if (SelectedModel == null) { StatusMessage = "Model required"; return false; }
            if (string.IsNullOrWhiteSpace(CountryOfOrigin)) { StatusMessage = "Country required"; return false; }
            return true;
        }
    }
}
