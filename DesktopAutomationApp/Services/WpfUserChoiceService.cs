using System.Windows;
using TaskAutomation.Steps;
using DesktopAutomationApp.Views;

namespace DesktopAutomationApp.Services;

public sealed class WpfUserChoiceService : IUserChoiceService
{
    public async Task<string?> ChooseAsync(
        UserChoiceDialogRequest request,
        CancellationToken cancellationToken)
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("The application dispatcher is not available.");

        var selectedOptionId = await dispatcher.InvokeAsync(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var dialog = new UserChoiceDialog(request)
            {
                Owner = Application.Current?.MainWindow
            };
            using var registration = cancellationToken.Register(() =>
                dispatcher.BeginInvoke(dialog.CancelFromJob));
            var accepted = dialog.ShowDialog() == true;
            return accepted ? dialog.SelectedOptionId : null;
        });

        // Do not throw from the WPF dispatcher callback. The JobExecutor already
        // handles cancellation on the normal async execution path as a clean stop.
        cancellationToken.ThrowIfCancellationRequested();
        return selectedOptionId;
    }
}
