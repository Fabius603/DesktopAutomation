using System.Windows;
using DesktopAutomationApp.Services;
using MahApps.Metro.Controls;

namespace DesktopAutomationApp.Views;

public partial class ReleaseNotesWindow : MetroWindow
{
    public ReleaseNotesWindow(IReadOnlyList<ReleaseNoteDisplay> notes)
    {
        InitializeComponent();
        DataContext = notes;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
