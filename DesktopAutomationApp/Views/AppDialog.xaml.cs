using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Media;

namespace DesktopAutomationApp.Views
{
    public partial class AppDialog : MetroWindow
    {
        private MessageBoxResult _result = MessageBoxResult.Cancel;

        private AppDialog(string title, string message, MessageBoxButton buttons, MessageBoxImage image)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
            ApplyIcon(image);
            ApplyButtons(buttons);
        }

        // ── Static factory (drop-in for MessageBox.Show) ──────────────────────

        public static MessageBoxResult Show(
            string message,
            string title,
            MessageBoxButton buttons,
            MessageBoxImage image,
            Window? owner = null)
        {
            var dlg = new AppDialog(title, message, buttons, image)
            {
                Owner = owner ?? Application.Current?.MainWindow
            };
            dlg.ShowDialog();
            return dlg._result;
        }

        // ── Icon ─────────────────────────────────────────────────────────────

        private void ApplyIcon(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Warning:
                    DialogIcon.Kind = PackIconMaterialKind.AlertCircleOutline;
                    DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));
                    break;
                case MessageBoxImage.Error:
                    DialogIcon.Kind = PackIconMaterialKind.CloseCircleOutline;
                    DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));
                    break;
                case MessageBoxImage.Information:
                    DialogIcon.Kind = PackIconMaterialKind.InformationOutline;
                    DialogIcon.Foreground = (Brush)FindResource("App.Brush.Accent");
                    break;
                default:
                    DialogIcon.Kind = PackIconMaterialKind.HelpCircleOutline;
                    DialogIcon.Foreground = (Brush)FindResource("App.Brush.Accent");
                    break;
            }
        }

        // ── Buttons ───────────────────────────────────────────────────────────

        private void ApplyButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    YesButton.Style = (Style)FindResource("ButtonPrimary");
                    YesIcon.Kind = PackIconMaterialKind.Check;
                    YesText.Text = "OK";
                    _result = MessageBoxResult.OK; // default if closed via X
                    break;

                case MessageBoxButton.YesNo:
                    YesButton.Style = (Style)FindResource("ButtonPrimary");
                    YesIcon.Kind = PackIconMaterialKind.Check;
                    YesText.Text = "Ja";
                    NoButton.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNoCancel:
                    YesButton.Style = (Style)FindResource("ButtonPrimary");
                    YesIcon.Kind = PackIconMaterialKind.Check;
                    YesText.Text = "Ja";
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        // ── Click handlers ────────────────────────────────────────────────────

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            _result = YesText.Text == "OK" ? MessageBoxResult.OK : MessageBoxResult.Yes;
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.No;
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
