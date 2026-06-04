namespace DesktopAutomation.Application.Interfaces
{
    public interface IDialogService
    {
        Task<bool> ConfirmAsync(string message, string title);
        Task<bool?> ConfirmWithCancelAsync(string message, string title);
        Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null);
        void ShowError(string message, string title);
    }
}
