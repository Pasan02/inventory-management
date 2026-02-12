using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Services;
using inventory_management.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace inventory_management.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authenticationService;

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isPasswordVisible;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        private string _statusMessage = "Enter credentials.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public Action? LoginSucceeded { get; set; }

        public LoginViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public void Reset()
        {
            StatusMessage = "Enter credentials.";
            Username = string.Empty;
            Password = string.Empty;
            IsPasswordVisible = false;
        }

        [RelayCommand]
        private async Task Login()
        {
            StatusMessage = "Signing in...";
            
            // Client-side pre-validation for better UX
            if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Login failed: Username and Password required.";
                ModernMessageDialog.ShowError("Login failed: Username and Password required.", "Login Failed");
                return;
            }

            var result = await _authenticationService.LoginAsync(Username, Password);
            
            // If the service returns generic 'Username and password required', we might want to handle it or just show it.
            // But since we did pre-check, we focus on result.Message
            
            if (result.Success)
            {
                StatusMessage = "Login successful.";
                ModernMessageDialog.ShowSuccess("Login successful!", "Success");
                Password = string.Empty;
                LoginSucceeded?.Invoke();
            }
            else
            {
                StatusMessage = result.Message;
                ModernMessageDialog.ShowError(result.Message, "Login Failed");
            }
        }
    }
}
