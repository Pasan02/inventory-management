using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace inventory_management.Views
{
    public partial class ModernMessageDialog : Window
    {
        public enum MessageType
        {
            Information,
            Success,
            Warning,
            Error,
            Question
        }

        public enum MessageButtons
        {
            OK,
            OKCancel,
            YesNo
        }

        public ModernMessageDialog(string message, string title, MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.OK)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            
            TitleText.Text = title;
            MessageText.Text = message;
            
            ConfigureIcon(type);
            ConfigureButtons(buttons);
        }

        private void ConfigureIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Success:
                    IconPath.Data = Geometry.Parse("M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z");
                    IconPath.Fill = (SolidColorBrush)FindResource("AccentColor");
                    break;
                    
                case MessageType.Warning:
                    IconPath.Data = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");
                    IconPath.Fill = (SolidColorBrush)FindResource("WarningColor");
                    break;
                    
                case MessageType.Error:
                    IconPath.Data = Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z");
                    IconPath.Fill = (SolidColorBrush)FindResource("DangerColor");
                    break;
                    
                case MessageType.Question:
                    IconPath.Data = Geometry.Parse("M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z");
                    IconPath.Fill = (SolidColorBrush)FindResource("PrimaryColor");
                    break;
                    
                default: // Information
                    IconPath.Data = Geometry.Parse("M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z");
                    IconPath.Fill = (SolidColorBrush)FindResource("PrimaryColor");
                    break;
            }
        }

        private void ConfigureButtons(MessageButtons buttons)
        {
            ButtonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageButtons.OKCancel:
                    AddButton("OK", true, true);
                    AddButton("Cancel", false, false);
                    break;
                    
                case MessageButtons.YesNo:
                    AddButton("Yes", true, true);
                    AddButton("No", false, false);
                    break;
                    
                default: // OK
                    AddButton("OK", true, true);
                    break;
            }
        }

        private void AddButton(string content, bool isDefault, bool? dialogResult)
        {
            var button = new Button
            {
                Content = content,
                Width = 120,
                Height = 44,
                Margin = new Thickness(8, 0, 8, 0),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                IsDefault = isDefault,
                IsCancel = !isDefault
            };

            button.Click += (s, e) =>
            {
                DialogResult = dialogResult;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }

        public static bool? Show(string message, string title, MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.OK)
        {
            var dialog = new ModernMessageDialog(message, title, type, buttons);
            return dialog.ShowDialog();
        }

        public static void ShowInformation(string message, string title = "Information")
        {
            Show(message, title, MessageType.Information, MessageButtons.OK);
        }

        public static void ShowSuccess(string message, string title = "Success")
        {
            Show(message, title, MessageType.Success, MessageButtons.OK);
        }

        public static void ShowWarning(string message, string title = "Warning")
        {
            Show(message, title, MessageType.Warning, MessageButtons.OK);
        }

        public static void ShowError(string message, string title = "Error")
        {
            Show(message, title, MessageType.Error, MessageButtons.OK);
        }

        public static bool? ShowQuestion(string message, string title = "Confirm")
        {
            return Show(message, title, MessageType.Question, MessageButtons.YesNo);
        }
    }
}
