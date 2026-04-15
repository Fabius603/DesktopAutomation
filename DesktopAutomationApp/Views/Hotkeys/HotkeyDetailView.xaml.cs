using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Views
{
    public partial class HotkeyDetailView : UserControl
    {
        public HotkeyDetailView() => InitializeComponent();

        private void ComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox cb && !cb.IsDropDownOpen)
            {
                cb.Focus();
                cb.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void ComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is ComboBox cb && !cb.IsDropDownOpen)
            {
                cb.IsDropDownOpen = true;
            }
        }
    }
}
