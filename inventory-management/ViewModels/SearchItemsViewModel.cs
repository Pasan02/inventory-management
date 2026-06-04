using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Data;
using inventory_management.Services;
using inventory_management.ViewModels.Search;

namespace inventory_management.ViewModels
{
    public partial class SearchItemsViewModel : ViewModelBase
    {
        private readonly InventoryDbContext _context;
        private readonly IDatabaseAvailabilityService _availabilityService;
        private readonly IPrintService _printService;

        private ViewModelBase _currentStep;
        public ViewModelBase CurrentStep
        {
            get => _currentStep;
            private set => SetProperty(ref _currentStep, value);
        }

        private PartTypeSearchRow? _selectedPart;
        private ManufacturerSearchRow? _selectedManufacturer;

        public SearchItemsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService, IPrintService printService)
        {
            _context = context;
            _availabilityService = availabilityService;
            _printService = printService;
            _currentStep = CreatePartsStep();
        }

        /// <summary>
        /// Handles hierarchical back navigation within the search pages.
        /// Returns true if navigated back within search hierarchy, false if at root (Parts page).
        /// </summary>
        public bool GoBack()
        {
            // If we're on All Items page, go back to Models
            if (CurrentStep is SearchAllItemsViewModel)
            {
                if (_selectedPart != null && _selectedManufacturer != null)
                {
                    CurrentStep = CreateModelsStep(_selectedPart, _selectedManufacturer);
                    return true;
                }
            }
            // If we're on the Models page, go back to Manufacturers
            else if (CurrentStep is SearchModelsViewModel)
            {
                if (_selectedPart != null)
                {
                    CurrentStep = CreateManufacturersStep(_selectedPart);
                    _selectedManufacturer = null;
                    return true;
                }
            }
            // If we're on the Manufacturers page, go back to Parts
            else if (CurrentStep is SearchManufacturersViewModel)
            {
                CurrentStep = CreatePartsStep();
                _selectedPart = null;
                return true;
            }
            // If we're on the Parts page, return false to indicate we should go to home
            else if (CurrentStep is SearchPartsViewModel)
            {
                return false;
            }

            return false;
        }

        [RelayCommand]
        private void Refresh()
        {
            if (CurrentStep is SearchPartsViewModel partsVm)
            {
                partsVm.LoadPartsCommand.Execute(null);
            }
            else if (CurrentStep is SearchManufacturersViewModel manufacturersVm)
            {
                manufacturersVm.LoadManufacturersCommand.Execute(null);
            }
            else if (CurrentStep is SearchModelsViewModel modelsVm)
            {
                modelsVm.LoadModelsCommand.Execute(null);
            }
            else if (CurrentStep is SearchAllItemsViewModel itemsVm)
            {
                itemsVm.LoadItemsCommand.Execute(null);
            }
        }

        private ViewModelBase CreatePartsStep()
        {
            return new SearchPartsViewModel(_context, _availabilityService, part =>
            {
                _selectedPart = part;
                CurrentStep = CreateManufacturersStep(part);
            });
        }

        private ViewModelBase CreateManufacturersStep(PartTypeSearchRow part)
        {
            return new SearchManufacturersViewModel(_context, _availabilityService, part, manufacturer =>
            {
                _selectedManufacturer = manufacturer;
                CurrentStep = CreateModelsStep(part, manufacturer);
            }, () => CurrentStep = CreatePartsStep());
        }

        private ViewModelBase CreateModelsStep(PartTypeSearchRow part, ManufacturerSearchRow manufacturer)
        {
            return new SearchModelsViewModel(_context, _availabilityService, part, manufacturer, () =>
            {
                CurrentStep = CreateManufacturersStep(part);
            }, () => 
            {
                CurrentStep = CreateAllItemsStep(part, manufacturer);
            });
        }

        private ViewModelBase CreateAllItemsStep(PartTypeSearchRow part, ManufacturerSearchRow manufacturer)
        {
            return new SearchAllItemsViewModel(_context, _availabilityService, _printService, part, manufacturer, () =>
            {
                CurrentStep = CreateModelsStep(part, manufacturer);
            });
        }
    }
}

