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

        private bool _isResetMode;
        public bool IsResetMode
        {
            get => _isResetMode;
            set => SetProperty(ref _isResetMode, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private string _currentPassword = string.Empty;
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
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
                // If both are potentially wrong (e.g. user exists but pass wrong, or user doesn't exist), 
                // the service now returns specific messages.
                // However, user asked: "if both are incorrect login failed"
                // Logic: 
                // 1. If username doesn't exist -> Service returns "Your username is incorrect."
                // 2. If username exists but password wrong -> Service returns "Your password is incorrect."
                // 3. If "both are incorrect" -> This is logically implied if the user types a wrong username AND a wrong password. 
                //    Since we check username first, we'll see "Your username is incorrect".
                //    If the user meant "If I type a random username AND random password", then "Username is incorrect" is the technically correct first error.
                
                StatusMessage = result.Message;
                ModernMessageDialog.ShowError(result.Message, "Login Failed");
            }
        }

        [RelayCommand]
        private void ToggleResetMode()
        {
            IsResetMode = !IsResetMode;
            Password = string.Empty;
            CurrentPassword = string.Empty;
            ConfirmPassword = string.Empty;
            StatusMessage = IsResetMode ? "Enter new credentials." : "Enter credentials.";
        }

        [RelayCommand]
        private async Task SubmitReset()
        {
            StatusMessage = "Updating password...";
            
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please provide your Username, Current Password, and New Password.";
                ModernMessageDialog.ShowError(StatusMessage, "Validation Error");
                return;
            }

            if (Password != ConfirmPassword)
            {
                StatusMessage = "Passwords do not match.";
                ModernMessageDialog.ShowError(StatusMessage, "Validation Error");
                return;
            }

            var loginResult = await _authenticationService.LoginAsync(Username, CurrentPassword);
            if (!loginResult.Success)
            {
                StatusMessage = "Current username or password is incorrect.";
                ModernMessageDialog.ShowError(StatusMessage, "Authentication Failed");
                return;
            }

            try
            {
                await _authenticationService.ForceSetPasswordAsync(Username, Password);
                StatusMessage = "Password successfully updated. Please sign in.";
                ModernMessageDialog.ShowSuccess(StatusMessage, "Success");
                IsResetMode = false;
                Password = string.Empty;
                CurrentPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                ModernMessageDialog.ShowError(ex.Message, "Update Failed");
            }
        }
    }
}
