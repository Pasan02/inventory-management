using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Views;
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace inventory_management.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private string _title = "Vehicle A/C Inventory System";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        private ViewModelBase? _currentViewModel;
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly LoginViewModel _loginViewModel;
        private IServiceScope? _currentScope;

        public MainViewModel(IServiceProvider serviceProvider, LoginViewModel loginViewModel)
        {
            _serviceProvider = serviceProvider;
            _loginViewModel = loginViewModel;
            _loginViewModel.LoginSucceeded = OnLoginSucceeded;
            CurrentViewModel = _loginViewModel;
            Title = "Sign In";
        }

        private void NavigateTo<TViewModel>(string title) where TViewModel : ViewModelBase
        {
            _currentScope?.Dispose();
            _currentScope = _serviceProvider.CreateScope();
            CurrentViewModel = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
            Title = title;
        }

        [RelayCommand]
        private void GoHome()
        {
            if (!IsAuthenticated)
            {
                CurrentViewModel = _loginViewModel;
                Title = "Sign In";
                return;
            }

            // Check if we're in the search hierarchy and need to go back one step
            if (CurrentViewModel is SearchItemsViewModel searchViewModel)
            {
                if (searchViewModel.GoBack())
                {
                    // Successfully navigated back within search hierarchy
                    return;
                }
            }

            // Otherwise go to home
            NavigateTo<HomeViewModel>("Vehicle A/C Inventory System");
        }

        [RelayCommand]
        private void NavigateToCreateItem()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            NavigateTo<ItemCreationViewModel>("Create New Inventory Item");
        }

        [RelayCommand]
        private void NavigateToAddStock()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            NavigateTo<AddStockViewModel>("Add Stock");
        }

        [RelayCommand]
        private void NavigateToRemoveStock()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            NavigateTo<RemoveStockViewModel>("Remove Stock");
        }

        [RelayCommand]
        private void NavigateToSearch()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            NavigateTo<SearchItemsViewModel>("Search / Items");
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            NavigateTo<ReportsViewModel>("Reports");
        }

        [RelayCommand]
        private void Logout()
        {
            var result = ModernMessageDialog.ShowQuestion(
                "Are you sure you want to sign out?", 
                "Confirm Sign Out");
                
            if (result == true)
            {
                _loginViewModel.Reset();
                IsAuthenticated = false;
                
                _currentScope?.Dispose();
                _currentScope = null;
                
                CurrentViewModel = _loginViewModel;
                Title = "Sign In";
            }
        }

        private void OnLoginSucceeded()
        {
            IsAuthenticated = true;
            GoHome();
        }
    }
}
