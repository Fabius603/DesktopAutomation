using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Views
{
    public partial class AutomationDetailView : UserControl
    {
        public AutomationDetailView() => InitializeComponent();

        private void Picker_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Navigation zwischen Feldern bleibt möglich; Datum und Uhrzeit werden
            // ausschließlich über das jeweilige Auswahl-Popup geändert.
            if (e.Key is Key.Tab or Key.LeftShift or Key.RightShift)
                return;

            e.Handled = true;
        }
    }
}
