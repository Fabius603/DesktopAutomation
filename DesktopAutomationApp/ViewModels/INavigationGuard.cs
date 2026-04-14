using System.Threading.Tasks;

namespace DesktopAutomationApp.ViewModels
{
    /// <summary>
    /// Implemented by ViewModels that have unsaved changes and need to guard navigation.
    /// </summary>
    public interface INavigationGuard
    {
        bool HasUnsavedChanges { get; }
        Task SaveAsync();
        void DiscardChanges();
    }
}
