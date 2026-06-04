using DesktopAutomationApp.Models;
using DesktopAutomationApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Infrastructure
{
    public sealed class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _services;

        public ViewModelFactory(IServiceProvider services)
        {
            _services = services;
        }

        public JobStepsViewModel CreateJobStepsViewModel(Job job)
            => ActivatorUtilities.CreateInstance<JobStepsViewModel>(_services, job);

        public MakroStepsViewModel CreateMakroStepsViewModel(Makro makro)
            => ActivatorUtilities.CreateInstance<MakroStepsViewModel>(_services, makro);

        public HotkeyDetailViewModel CreateHotkeyDetailViewModel(EditableHotkey hotkey)
            => ActivatorUtilities.CreateInstance<HotkeyDetailViewModel>(_services, hotkey);
    }
}
