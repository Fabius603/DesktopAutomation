using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using DesktopAutomationApp.Services.Jobs;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class JobOpeningExceptionTests
{
    [Fact]
    public void StepDetails_DoNotUseInvalidOperationExceptionsForJsonConversion()
    {
        var original = new StartProcessStep
        {
            Settings = new StartProcessSettings
            {
                ExecutablePath = @"C:\Tools\worker.exe",
                WaitForExit = true,
                PlacementMode = StartProcessPlacementMode.Custom,
                MonitorIndex = 2
            }
        };
        var step = JsonSerializer.Deserialize<StartProcessStep>(
            JsonSerializer.Serialize(original))!;
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is InvalidOperationException
                && args.Exception.StackTrace?.Contains(
                    nameof(JobStepDetailsProvider), StringComparison.Ordinal) == true)
                exceptions.Enqueue(args.Exception);
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            var provider = new JobStepDetailsProvider();
            _ = provider.GetSummary(step, new JobStep[] { step });
            _ = provider.GetDetails(step, new JobStep[] { step });
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task SupersededValidationDelay_CompletesWithoutTaskCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        var exceptions = new ConcurrentQueue<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception is TaskCanceledException
                && args.Exception.StackTrace?.Contains(
                    nameof(JobStepsViewModel), StringComparison.Ordinal) == true)
                exceptions.Enqueue(args.Exception);
        };

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            var wait = JobStepsViewModel.WaitForValidationDebounceAsync(
                cancellation.Token, delayMilliseconds: 10);
            cancellation.Cancel();

            Assert.False(await wait);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        Assert.Empty(exceptions);
    }
}
