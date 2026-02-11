using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public MainViewModel(IServiceProvider serviceProvider, LoginViewModel loginViewModel)
        {
            _serviceProvider = serviceProvider;
            _loginViewModel = loginViewModel;
            _loginViewModel.LoginSucceeded = OnLoginSucceeded;
            CurrentViewModel = _loginViewModel;
            Title = "Sign In";
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

            CurrentViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
            Title = "Vehicle A/C Inventory System";
        }

        [RelayCommand]
        private void NavigateToCreateItem()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<ItemCreationViewModel>();
             Title = "Create New Inventory Item";
        }

        [RelayCommand]
        private void NavigateToAddStock()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<AddStockViewModel>();
            Title = "Add Stock";
        }

        [RelayCommand]
        private void NavigateToRemoveStock()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<RemoveStockViewModel>();
            Title = "Remove Stock";
        }

        [RelayCommand]
        private void NavigateToSearch()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<SearchItemsViewModel>();
            Title = "Search / Items";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            if (!IsAuthenticated)
            {
                GoHome();
                return;
            }

            CurrentViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
            Title = "Reports";
        }

        [RelayCommand]
        private void Logout()
        {
            var result = MessageBox.Show(Application.Current.MainWindow, 
                                         "Are you sure you want to sign out?", 
                                         "Confirm Sign Out", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _loginViewModel.Reset();
                IsAuthenticated = false;
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
