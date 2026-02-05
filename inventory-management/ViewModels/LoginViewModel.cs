using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using inventory_management.Services;
using System;
using System.Threading.Tasks;

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

        [RelayCommand]
        private async Task Login()
        {
            StatusMessage = "Signing in...";
            var result = await _authenticationService.LoginAsync(Username, Password);
            StatusMessage = result.Message;

            if (result.Success)
            {
                Password = string.Empty;
                LoginSucceeded?.Invoke();
            }
        }
    }
}
