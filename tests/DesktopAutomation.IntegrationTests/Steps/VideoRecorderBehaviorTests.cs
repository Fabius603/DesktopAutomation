using System.Diagnostics;
using System.Drawing;

namespace TaskAutomation.Tests.Steps;

public sealed class VideoRecorderBehaviorTests
{
    [Fact]
    public async Task CancelledInitialization_NeverLaunchesEncoder()
    {
        var initialized = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var launches = 0;
        using var recorder = new StreamVideoRecorder(2, 2, 60, initialization: initialized.Task,
            startProcess: _ => { launches++; throw new InvalidOperationException("unexpected launch"); });
        using var stop = new CancellationTokenSource();
        var start = recorder.StartAsync(stop.Token);
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(TimeSpan.FromSeconds(5)));
        initialized.SetResult();
        Assert.Equal(0, launches);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(0)]
    public async Task EncoderFailureOrMissingOutput_IsNotReportedAsSaved(int exitCode)
    {
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var recorder = new StreamVideoRecorder(2, 2, 60, initialization: Task.CompletedTask, startProcess: settings =>
        {
            settings.FileName = "powershell.exe";
            settings.Arguments = $"-NoProfile -Command \"[Console]::OpenStandardInput().CopyTo([System.IO.Stream]::Null); exit {exitCode}\"";
            var process = Process.Start(settings)!;
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => exited.TrySetResult();
            if (process.HasExited) exited.TrySetResult();
            return process;
        });
        await recorder.StartAsync(default);
        using var bitmap = new Bitmap(2, 2);
        recorder.AddFrame(bitmap);
        await Assert.ThrowsAsync<IOException>(() => recorder.StopAndSave().WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsAsync<IOException>(() => recorder.StopAndSave());
    }
}
