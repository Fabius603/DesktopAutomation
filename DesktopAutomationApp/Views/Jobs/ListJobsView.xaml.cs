using DesktopAutomationApp.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopAutomationApp.Views
{
    public partial class ListJobsView : UserControl
    {
        public ListJobsView() => InitializeComponent();

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is TaskAutomation.Jobs.Job job
                && dg.DataContext is ListJobsViewModel vm)
            {
                vm.OpenJobCommand.Execute(job);
            }
        }
    }
}
