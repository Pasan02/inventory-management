using System.Collections.ObjectModel;
using System.Windows;
using inventory_management.Data;
using inventory_management.Data.Entities;
using inventory_management.Services;
using inventory_management.Views;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace inventory_management.ViewModels
{
    public partial class ItemCreationViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IBarcodeService _barcodeService;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private bool _isAddingReferenceData;

        // --- Form Properties ---
        
        private string _selectedBarcode = string.Empty;
        public string SelectedBarcode
        {
            get => _selectedBarcode;
            set => SetProperty(ref _selectedBarcode, value);
        }

        [RelayCommand]
        private async Task BrowsePartTypeImage()
        {
            // Check if a part type is selected
            if (SelectedPartType == null || SelectedPartType.Id == -1)
            {
                StatusMessage = "Please select a part type first.";
                ModernMessageDialog.ShowWarning("Please select a part type first.", "Validation Error");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                StatusMessage = "Saving image...";

                // Read the image bytes
                byte[] imageBytes = File.ReadAllBytes(dialog.FileName);

                // Copy the file to assets folder
                var assetsRoot = AssetPathService.BasePath;
                Directory.CreateDirectory(Path.Combine(assetsRoot, "part-types"));

                var extension = Path.GetExtension(dialog.FileName);
                var fileName = $"part-type-{Guid.NewGuid():N}{extension}";
                var relativePath = Path.Combine("part-types", fileName);
                var destination = Path.Combine(assetsRoot, relativePath);

                File.Copy(dialog.FileName, destination, true);

                // Update the database
                var partType = await _context.PartTypes.FindAsync(SelectedPartType.Id);
                if (partType != null)
                {
                    partType.ImagePath = relativePath;
                    partType.Image = imageBytes;
                    await _context.SaveChangesAsync();

                    // Update the selected item
                    SelectedPartType.ImagePath = relativePath;
                    SelectedPartType.Image = imageBytes;

                    StatusMessage = "Image saved successfully.";
                    ModernMessageDialog.ShowSuccess("Image saved successfully!", "Success");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving image: {ex.Message}";
                ModernMessageDialog.ShowError($"Error saving image: {ex.Message}", "Error");
            }
        }

        private ImageSource? _barcodeImage;
        public ImageSource? BarcodeImage
        {
            get => _barcodeImage;
            set => SetProperty(ref _barcodeImage, value);
        }

        [RelayCommand]
        private async Task BrowseManufacturerLogo()
        {
            // Check if a manufacturer is selected
            if (SelectedManufacturer == null || SelectedManufacturer.Id == -1)
            {
                StatusMessage = "Please select a manufacturer first.";
                ModernMessageDialog.ShowWarning("Please select a manufacturer first.", "Validation Error");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                StatusMessage = "Saving logo...";

                // Read the image bytes
                byte[] logoBytes = File.ReadAllBytes(dialog.FileName);

                // Copy the file to assets folder
                var assetsRoot = AssetPathService.BasePath;
                Directory.CreateDirectory(Path.Combine(assetsRoot, "manufacturers"));

                var extension = Path.GetExtension(dialog.FileName);
                var fileName = $"manufacturer-{Guid.NewGuid():N}{extension}";
                var relativePath = Path.Combine("manufacturers", fileName);
                var destination = Path.Combine(assetsRoot, relativePath);

                File.Copy(dialog.FileName, destination, true);

                // Update the database
                var manufacturer = await _context.Manufacturers.FindAsync(SelectedManufacturer.Id);
                if (manufacturer != null)
                {
                    manufacturer.LogoPath = relativePath;
                    manufacturer.Logo = logoBytes;
                    await _context.SaveChangesAsync();

                    // Update the selected item
                    SelectedManufacturer.LogoPath = relativePath;
                    SelectedManufacturer.Logo = logoBytes;

                    StatusMessage = "Logo saved successfully.";
                    ModernMessageDialog.ShowSuccess("Logo saved successfully!", "Success");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving logo: {ex.Message}";
                ModernMessageDialog.ShowError($"Error saving logo: {ex.Message}", "Error");
            }
        }

        [RelayCommand]
        private void BrowseItemImage()
        {
            var relativePath = TryPickImage("items", "item");
            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                ImagePath = relativePath;
            }
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

        private string _newPartTypeName = string.Empty;
        public string NewPartTypeName
        {
            get => _newPartTypeName;
            set => SetProperty(ref _newPartTypeName, value);
        }

        private string _newPartTypeImagePath = string.Empty;
        public string NewPartTypeImagePath
        {
            get => _newPartTypeImagePath;
            set => SetProperty(ref _newPartTypeImagePath, value);
        }

        private PartType? _selectedPartType;
        public PartType? SelectedPartType
        {
            get => _selectedPartType;
            set
            {
                if (value?.Id == -1)
                {
                    if (_isAddingReferenceData) return;
                    OnPropertyChanged(nameof(SelectedPartType));
                    Application.Current.Dispatcher.InvokeAsync(HandleAddPartType);
                    return; 
                }
                SetProperty(ref _selectedPartType, value);
            }
        }


        public ObservableCollection<VehicleManufacturer> Manufacturers { get; } = new();

        private string _newManufacturerName = string.Empty;
        public string NewManufacturerName
        {
            get => _newManufacturerName;
            set => SetProperty(ref _newManufacturerName, value);
        }

        private string _newManufacturerLogoPath = string.Empty;
        public string NewManufacturerLogoPath
        {
            get => _newManufacturerLogoPath;
            set => SetProperty(ref _newManufacturerLogoPath, value);
        }

        private VehicleManufacturer? _selectedManufacturer;
        public VehicleManufacturer? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set 
            {
                if (value?.Id == -1)
                {
                    if (_isAddingReferenceData) return;
                    OnPropertyChanged(nameof(SelectedManufacturer));
                    Application.Current.Dispatcher.InvokeAsync(HandleAddManufacturer);
                    return;
                }

                if (SetProperty(ref _selectedManufacturer, value))
                {
                    OnSelectedManufacturerChanged(value);
                }
            }
        }

        public ObservableCollection<VehicleModel> Models { get; } = new();

        private string _newModelName = string.Empty;
        public string NewModelName
        {
            get => _newModelName;
            set => SetProperty(ref _newModelName, value);
        }

        private string _newModelYearRange = string.Empty;
        public string NewModelYearRange
        {
            get => _newModelYearRange;
            set => SetProperty(ref _newModelYearRange, value);
        }
        
        private VehicleModel? _selectedModel;
        public VehicleModel? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (value?.Id == -1)
                {
                    if (_isAddingReferenceData) return;
                    OnPropertyChanged(nameof(SelectedModel));
                    Application.Current.Dispatcher.InvokeAsync(HandleAddModel);
                    return;
                }
                SetProperty(ref _selectedModel, value);
            }
        }

        public ObservableCollection<PartBrand> Brands { get; } = new();
        
        private PartBrand? _selectedBrand;
        public PartBrand? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (value?.Id == -1)
                {
                    if (_isAddingReferenceData) return;
                    OnPropertyChanged(nameof(SelectedBrand));
                    Application.Current.Dispatcher.InvokeAsync(HandleAddBrand);
                    return;
                }
                SetProperty(ref _selectedBrand, value);
            }
        }
        
        public ObservableCollection<Rack> Racks { get; } = new();
        
        private Rack? _selectedRack;
        public Rack? SelectedRack
        {
            get => _selectedRack;
            set
            {
                if (value?.Id == -1)
                {
                    if (_isAddingReferenceData) return;
                    OnPropertyChanged(nameof(SelectedRack));
                    Application.Current.Dispatcher.InvokeAsync(HandleAddRack);
                    return;
                }
                SetProperty(ref _selectedRack, value);
            }
        }

        // --- UI State ---
        private string _statusMessage = string.Empty;
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

        [RelayCommand]
        private async void LoadReferenceData()
        {
            try
            {
                StatusMessage = "Loading data...";
                
                var availability = await _availabilityService.GetStatusAsync();
                if (!availability.IsAvailable)
                {
                    StatusMessage = availability.Message;
                    return;
                }

                // Clear existing collections
                PartTypes.Clear();
                Brands.Clear();
                Manufacturers.Clear();
                Racks.Clear();
                Models.Clear();
                
                // Load lookups
                var types = await _context.PartTypes.OrderBy(t => t.Name).ToListAsync();
                foreach (var t in types) PartTypes.Add(t);
                PartTypes.Add(new PartType { Id = -1, Name = "+ Add New Part Type..." });

                var brands = await _context.PartBrands.OrderBy(b => b.Name).ToListAsync();
                foreach (var b in brands) Brands.Add(b);
                Brands.Add(new PartBrand { Id = -1, Name = "+ Add New Brand..." });

                var manufacturers = await _context.Manufacturers.OrderBy(m => m.Name).ToListAsync();
                foreach (var m in manufacturers) Manufacturers.Add(m);
                Manufacturers.Add(new VehicleManufacturer { Id = -1, Name = "+ Add New Manufacturer..." });

                var racks = await _context.Racks.OrderBy(r => r.LocationCode).ToListAsync();
                foreach (var r in racks) Racks.Add(r);
                Racks.Add(new Rack { Id = -1, LocationCode = "+ Add New Rack..." });
                
                // Ensure Models ComboBox is not empty initially
                Models.Add(new VehicleModel { Id = -1, Name = "+ Add New Model..." });

                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }
        }

        private async void OnSelectedManufacturerChanged(VehicleManufacturer? value)
        {
            try
            {
                Models.Clear();
                SelectedModel = null;
                
                if (value != null && value.Id != -1)
                {
                    var models = await _context.Models
                        .Where(m => m.VehicleManufacturerId == value.Id)
                        .OrderBy(m => m.Name)
                        .ToListAsync();
                    
                    foreach (var m in models) Models.Add(m);
                }
                
                // Always add the "Add New Model" option so the dropdown isn't empty
                Models.Add(new VehicleModel { Id = -1, Name = "+ Add New Model..." });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading models: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddPartType()
        {
            if (string.IsNullOrWhiteSpace(NewPartTypeName))
            {
                StatusMessage = "Part type name is required.";
                return;
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            var name = NewPartTypeName.Trim();
            if (await _context.PartTypes.AnyAsync(p => p.Name == name))
            {
                StatusMessage = "Part type already exists.";
                return;
            }

            var partType = new PartType
            {
                Name = name,
                ImagePath = string.IsNullOrWhiteSpace(NewPartTypeImagePath) ? null : NewPartTypeImagePath.Trim()
            };
            _context.PartTypes.Add(partType);
            await _context.SaveChangesAsync();

            PartTypes.Add(partType);
            NewPartTypeName = string.Empty;
            NewPartTypeImagePath = string.Empty;
            StatusMessage = "Part type added.";
        }

        [RelayCommand]
        private async Task AddManufacturer()
        {
            if (string.IsNullOrWhiteSpace(NewManufacturerName))
            {
                StatusMessage = "Manufacturer name is required.";
                return;
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            var name = NewManufacturerName.Trim();
            if (await _context.Manufacturers.AnyAsync(m => m.Name == name))
            {
                StatusMessage = "Manufacturer already exists.";
                return;
            }

            var manufacturer = new VehicleManufacturer
            {
                Name = name,
                LogoPath = string.IsNullOrWhiteSpace(NewManufacturerLogoPath) ? null : NewManufacturerLogoPath.Trim()
            };
            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();

            Manufacturers.Add(manufacturer);
            NewManufacturerName = string.Empty;
            NewManufacturerLogoPath = string.Empty;
            StatusMessage = "Manufacturer added.";
        }

        [RelayCommand]
        private async Task AddModel()
        {
            if (SelectedManufacturer == null)
            {
                StatusMessage = "Select a manufacturer for the model.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewModelName))
            {
                StatusMessage = "Model name is required.";
                return;
            }

            var availability = await _availabilityService.GetStatusAsync();
            if (!availability.IsAvailable)
            {
                StatusMessage = availability.Message;
                return;
            }

            var name = NewModelName.Trim();
            if (await _context.Models.AnyAsync(m => m.VehicleManufacturerId == SelectedManufacturer.Id && m.Name == name))
            {
                StatusMessage = "Model already exists for this manufacturer.";
                return;
            }

            var model = new VehicleModel
            {
                VehicleManufacturerId = SelectedManufacturer.Id,
                Name = name,
                YearRange = string.IsNullOrWhiteSpace(NewModelYearRange) ? null : NewModelYearRange.Trim()
            };

            _context.Models.Add(model);
            await _context.SaveChangesAsync();

            Models.Add(model);
            NewModelName = string.Empty;
            NewModelYearRange = string.Empty;
            StatusMessage = "Model added.";
        }

        [RelayCommand]
        private void Reset()
        {
            // Clear all form fields
            SelectedPartType = null;
            SelectedBrand = null;
            SelectedManufacturer = null;
            SelectedModel = null;
            SelectedRack = null;
            
            CountryOfOrigin = string.Empty;
            Description = string.Empty;
            ImagePath = string.Empty;
            LowStockThreshold = 5;
            
            NewModelName = string.Empty;
            NewModelYearRange = string.Empty;
            
            SelectedBarcode = string.Empty;
            BarcodeImage = null;
            StatusMessage = string.Empty;
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
                    Barcode = "TEMP-" + Guid.NewGuid().ToString().Substring(0,8),
                    ImagePath = string.IsNullOrWhiteSpace(ImagePath) ? null : ImagePath.Trim()
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

                ModernMessageDialog.ShowSuccess($"Item Saved Successfully!{Environment.NewLine}Barcode: {newItem.Barcode}", "Success");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
                ModernMessageDialog.ShowError($"Error saving item: {ex.Message}", "Error");
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
            if (SelectedPartType == null) 
            { 
                StatusMessage = string.Empty; // Clear text message, handled by dialog or validation visual 
                ModernMessageDialog.ShowWarning("Part Type is required.", "Validation Error");
                return false; 
            }
            if (SelectedBrand == null) 
            { 
                StatusMessage = string.Empty;
                ModernMessageDialog.ShowWarning("Brand is required.", "Validation Error");
                return false; 
            }
            if (SelectedManufacturer == null) 
            { 
                StatusMessage = string.Empty;
                ModernMessageDialog.ShowWarning("Manufacturer is required.", "Validation Error");
                return false; 
            }
            if (SelectedModel == null) 
            { 
                StatusMessage = string.Empty;
                ModernMessageDialog.ShowWarning("Model is required.", "Validation Error");
                return false; 
            }
            if (string.IsNullOrWhiteSpace(CountryOfOrigin)) 
            { 
                StatusMessage = string.Empty;
                ModernMessageDialog.ShowWarning("Country of Origin is required.", "Validation Error");
                return false; 
            }
            return true;
        }

        private string? TryPickImage(string subfolder, string prefix)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            var assetsRoot = AssetPathService.BasePath;
            Directory.CreateDirectory(Path.Combine(assetsRoot, subfolder));

            var extension = Path.GetExtension(dialog.FileName);
            var fileName = $"{prefix}-{Guid.NewGuid():N}{extension}";
            var relativePath = Path.Combine(subfolder, fileName);
            var destination = Path.Combine(assetsRoot, relativePath);

            File.Copy(dialog.FileName, destination, true);

            return relativePath;
        }

        private async void HandleAddPartType()
        {
            _isAddingReferenceData = true;
            try
            {
                var dialog = new SimpleInputDialog("Add Part Type", "Enter new part type name:");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                {
                    try 
                    {
                        var name = dialog.InputValue.Trim();
                        if (PartTypes.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            StatusMessage = "Part Type already exists.";
                            ModernMessageDialog.ShowWarning("Part Type already exists.", "Error");
                            return;
                        }

                        var newItem = new PartType { Name = name };
                        _context.PartTypes.Add(newItem);
                        await _context.SaveChangesAsync();

                        PartTypes.Insert(PartTypes.Count - 1, newItem);
                        SelectedPartType = newItem;
                        
                        ModernMessageDialog.ShowSuccess("Part Type added successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error adding Part Type: {ex.Message}";
                        ModernMessageDialog.ShowError($"Error adding Part Type: {ex.Message}", "Error");
                    }
                }
            }
            finally
            {
                _isAddingReferenceData = false;
            }
        }

        private async void HandleAddManufacturer()
        {
            _isAddingReferenceData = true;
            try
            {
                var dialog = new SimpleInputDialog("Add Manufacturer", "Enter new manufacturer name:");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                {
                    try
                    {
                        var name = dialog.InputValue.Trim();
                        if (Manufacturers.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            StatusMessage = "Manufacturer already exists.";
                            ModernMessageDialog.ShowWarning("Manufacturer already exists.", "Error");
                            return;
                        }

                        var newItem = new VehicleManufacturer { Name = name };
                        _context.Manufacturers.Add(newItem);
                        await _context.SaveChangesAsync();

                        Manufacturers.Insert(Manufacturers.Count - 1, newItem);
                        SelectedManufacturer = newItem;
                        
                        ModernMessageDialog.ShowSuccess("Manufacturer added successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error adding Manufacturer: {ex.Message}";
                        ModernMessageDialog.ShowError($"Error adding Manufacturer: {ex.Message}", "Error");
                    }
                }
            }
            finally
            {
                _isAddingReferenceData = false;
            }
        }

        private async void HandleAddBrand()
        {
            _isAddingReferenceData = true;
            try
            {
                var dialog = new SimpleInputDialog("Add Brand", "Enter new brand name:");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                {
                    try
                    {
                        var name = dialog.InputValue.Trim();
                        if (Brands.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            StatusMessage = "Brand already exists.";
                            ModernMessageDialog.ShowWarning("Brand already exists.", "Error");
                            return;
                        }

                        var newItem = new PartBrand { Name = name };
                        _context.PartBrands.Add(newItem);
                        await _context.SaveChangesAsync();

                        Brands.Insert(Brands.Count - 1, newItem);
                        SelectedBrand = newItem;
                        
                        ModernMessageDialog.ShowSuccess("Brand added successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error adding Brand: {ex.Message}";
                        ModernMessageDialog.ShowError($"Error adding Brand: {ex.Message}", "Error");
                    }
                }
            }
            finally
            {
                _isAddingReferenceData = false;
            }
        }

        private async void HandleAddRack()
        {
            _isAddingReferenceData = true;
            try
            {
                var dialog = new SimpleInputDialog("Add Rack", "Enter new rack location code:");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                {
                    try
                    {
                        var code = dialog.InputValue.Trim();
                        if (Racks.Any(x => x.LocationCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                        {
                            StatusMessage = "Rack already exists.";
                            ModernMessageDialog.ShowWarning("Rack already exists.", "Error");
                            return;
                        }

                        var newItem = new Rack { LocationCode = code };
                        _context.Racks.Add(newItem);
                        await _context.SaveChangesAsync();

                        Racks.Insert(Racks.Count - 1, newItem);
                        SelectedRack = newItem;
                        
                        ModernMessageDialog.ShowSuccess("Rack added successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error adding Rack: {ex.Message}";
                        ModernMessageDialog.ShowError($"Error adding Rack: {ex.Message}", "Error");
                    }
                }
            }
            finally
            {
                _isAddingReferenceData = false;
            }
        }

        private async void HandleAddModel()
        {
            if (SelectedManufacturer == null || SelectedManufacturer.Id == -1)
            {
                StatusMessage = "Please select a manufacturer first.";
                ModernMessageDialog.ShowWarning("Please select a manufacturer before adding a model.", "Validation Error");
                // Reset selection so they can try again later
                SelectedModel = null;
                return;
            }

            _isAddingReferenceData = true;
            try
            {
                var dialog = new SimpleInputDialog("Add Model", $"Enter new model name for {SelectedManufacturer.Name}:");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
                {
                    try
                    {
                        var name = dialog.InputValue.Trim();
                        if (Models.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            StatusMessage = "Model already exists.";
                            ModernMessageDialog.ShowWarning("Model already exists for this manufacturer.", "Error");
                            return;
                        }

                        var newItem = new VehicleModel 
                        { 
                            Name = name, 
                            VehicleManufacturerId = SelectedManufacturer.Id
                        };
                        _context.Models.Add(newItem);
                        await _context.SaveChangesAsync();

                        Models.Insert(Models.Count - 1, newItem);
                        SelectedModel = newItem;
                        
                        ModernMessageDialog.ShowSuccess("Vehicle Model added successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                       StatusMessage = $"Error adding Model: {ex.Message}";
                       ModernMessageDialog.ShowError($"Error adding Model: {ex.Message}", "Error");
                    }
                }
            }
            finally
            {
                _isAddingReferenceData = false;
            }
        }
    }
}
