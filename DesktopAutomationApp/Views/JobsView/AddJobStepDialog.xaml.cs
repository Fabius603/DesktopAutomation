using DesktopAutomationApp.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DesktopAutomationApp.Views
{
    /// <summary>
    /// Interaktionslogik für AddJobStepDialog.xaml
    /// </summary>
    public partial class AddJobStepDialog : MetroWindow
    {
        public AddJobStepDialog()
        {
            InitializeComponent();
            Loaded += (_, __) => CenterOnOwnerOnce();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddJobStepDialogViewModel vm)
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

        private void CenterOnOwnerOnce()
        {
            if (Owner == null) return;
            UpdateLayout();
            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
        }
    }
}
