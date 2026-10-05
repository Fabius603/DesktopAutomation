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
            Loaded += async (_, __) =>
            {
                CenterOnOwnerOnce();
                StepSearchBox.Focus();
                if (DataContext is AddJobStepDialogViewModel vm)
                    await vm.InitializeAsync();
            };
        }
        private void StepTypeList_Loaded(object sender, RoutedEventArgs e) => RevealSelectedType();

        private void StepTypeList_SizeChanged(object sender, SizeChangedEventArgs e) => RevealSelectedType();

        private void StepTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e) => RevealSelectedType();

        private void RevealSelectedType()
        {
            // Bring into view after resized rows and the viewport have completed layout.
            StepTypeList.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                if (StepTypeList.SelectedItem is not null) StepTypeList.ScrollIntoView(StepTypeList.SelectedItem);
            }));
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
