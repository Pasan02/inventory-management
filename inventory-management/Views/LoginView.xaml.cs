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

        private void TogglePasswordVisibility(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.IsPasswordVisible = !viewModel.IsPasswordVisible;
                if (!viewModel.IsPasswordVisible)
                {
                    PasswordInput.Password = viewModel.Password;
                }
            }
        }
    }
}
