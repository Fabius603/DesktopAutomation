using DesktopAutomationApp.Models;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Infrastructure
{
    public interface IViewModelFactory
    {
        JobStepsViewModel CreateJobStepsViewModel(Job job);
        MakroStepsViewModel CreateMakroStepsViewModel(Makro makro);
        HotkeyDetailViewModel CreateHotkeyDetailViewModel(EditableHotkey hotkey);
    }
}
