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

        private ViewModelBase _currentStep;
        public ViewModelBase CurrentStep
        {
            get => _currentStep;
            private set => SetProperty(ref _currentStep, value);
        }

        private PartTypeSearchRow? _selectedPart;

        public SearchItemsViewModel(InventoryDbContext context, IDatabaseAvailabilityService availabilityService)
        {
            _context = context;
            _availabilityService = availabilityService;
            _currentStep = CreatePartsStep();
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
                CurrentStep = CreateModelsStep(part, manufacturer);
            }, () => CurrentStep = CreatePartsStep());
        }

        private ViewModelBase CreateModelsStep(PartTypeSearchRow part, ManufacturerSearchRow manufacturer)
        {
            return new SearchModelsViewModel(_context, _availabilityService, part, manufacturer, () =>
            {
                CurrentStep = CreateManufacturersStep(part);
            });
        }
    }
}
