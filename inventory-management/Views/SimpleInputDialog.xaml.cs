using System.Windows;

namespace inventory_management.Views
{
    public partial class SimpleInputDialog : Window
    {
        public string InputValue { get; private set; } = string.Empty;

        public SimpleInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputTextBox.Text;
            DialogResult = true;
        }
    }
}
