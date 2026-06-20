using System.Windows.Controls;
using inventory_management.ViewModels;

namespace inventory_management.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        private void OnConfirmPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmPassword = passwordBox.Password;
            }
        }

        private void OnCurrentPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.CurrentPassword = passwordBox.Password;
            }
        }

        private void TogglePasswordVisibility(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.IsPasswordVisible = !viewModel.IsPasswordVisible;
                if (!viewModel.IsPasswordVisible)
                {
                    PasswordInput.Password = viewModel.Password;
                    if (ConfirmPasswordInput != null)
                        ConfirmPasswordInput.Password = viewModel.ConfirmPassword;
                    if (CurrentPasswordInput != null)
                        CurrentPasswordInput.Password = viewModel.CurrentPassword;
                }
            }
        }
    }
}
