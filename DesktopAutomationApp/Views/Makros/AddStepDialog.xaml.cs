using System.Windows;
using DesktopAutomationApp.ViewModels;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp.Views
{
    public partial class AddStepDialog : MetroWindow
    {
        public AddStepDialog() => InitializeComponent();

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddStepDialogViewModel vm)
            {
                vm.CreateStep();
                DialogResult = vm.CreatedStep != null; // ShowDialog() gibt dann true/false zurück
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
