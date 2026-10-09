using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;
using TaskAutomation.Makros;
using TaskAutomation.Timing;
using WindowsInput.Native;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroExecutorTests
{

    [Fact]
    public async Task ConcurrentMacros_DoNotInterleaveOrReleaseAnotherMacrosKeys()
    {
        var input = new RecordingInputController();
        var delay = new BlockingDelay();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, delay, input);
        var first = executor.ExecuteMakro(Macro(new KeyDownBefehl { Key = "A" }, new TimeoutBefehl { Duration = 1 }), null!, default);
        await delay.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = executor.ExecuteMakro(Macro(new KeyDownBefehl { Key = "A" }), null!, default);
        Assert.False(second.IsCompleted);
        Assert.Equal(["key:VK_A:True"], input.Calls);
        delay.Release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(["key:VK_A:True", "key:VK_A:False", "key:VK_A:True", "key:VK_A:False"], input.Calls);
    }

    [Fact]
    public async Task CancellationWhileWaitingForMacro_ProducesNoInput()
    {
        var input = new RecordingInputController();
        var delay = new BlockingDelay();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, delay, input);
        var first = executor.ExecuteMakro(Macro(new KeyDownBefehl { Key = "A" }, new TimeoutBefehl { Duration = 1 }), null!, default);
        await delay.Entered.Task;
        using var cancellation = new CancellationTokenSource();
        var second = executor.ExecuteMakro(Macro(new KeyDownBefehl { Key = "B" }), null!, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        delay.Release.SetResult();
        await first;
        Assert.Equal(["key:VK_A:True", "key:VK_A:False"], input.Calls);
    }

    private sealed class BlockingDelay : IPreciseDelayService
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) => DelayUntilAsync(0, cancellationToken);
        public Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default)
        { Entered.TrySetResult(); return Release.Task.WaitAsync(cancellationToken); }
    }

    [Fact]
    public async Task ExecuteMakro_ReplaysRelativeMovementButtonsAndKeysInOrder()
    {
        var input = new RecordingInputController();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, new RecordingDelay(), input);
        var macro = Macro(
            new MouseMoveRelativeBefehl { DeltaX = 5, DeltaY = -3 },
            new MouseDownBefehl { Button = "Middle" },
            new MouseUpBefehl { Button = "Middle" },
            new MouseWheelBefehl { DeltaX = -120, DeltaY = 240 },
            new KeyDownBefehl { Key = "A" },
            new KeyUpBefehl { Key = "A" });
        await executor.ExecuteMakro(macro, null!, default);
        Assert.Equal(["relative:5:-3", "mouse:Middle:True", "mouse:Middle:False", "wheel:-120:240", "key:VK_A:True", "key:VK_A:False"], input.Calls);
    }

    [Fact]
    public async Task ExecuteMakro_UsesOneAbsoluteTimelineForPreciseAndLegacyDelays()
    {
        var delay = new RecordingDelay();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, delay, new RecordingInputController());
        await executor.ExecuteMakro(Macro(
            new MouseMoveRelativeBefehl { DeltaX = 1, DelayBeforeMicroseconds = 500 },
            new TimeoutBefehl { Duration = 2 },
            new MouseMoveRelativeBefehl { DeltaX = 1, DelayBeforeMicroseconds = 750 }), null!, default);
        Assert.Equal(3, delay.Targets.Count);
        Assert.True(delay.Targets[0] < delay.Targets[1]);
        Assert.True(delay.Targets[1] < delay.Targets[2]);
    }

    [Fact]
    public async Task ExecuteMakro_ReleasesHeldInputWhenCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        var input = new RecordingInputController();
        var delay = new RecordingDelay { OnDelay = () => cancellation.Cancel() };
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, delay, input);
        var macro = Macro(new MouseDownBefehl { Button = "Left" }, new TimeoutBefehl { Duration = 10 });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteMakro(macro, null!, cancellation.Token));
        Assert.Equal(["mouse:Left:True", "mouse:Left:False"], input.Calls);
    }

    [Fact]
    public async Task ExecuteMakro_InvalidKeyIsLoggedAndSkippedWithoutThrowing()
    {
        var input = new RecordingInputController();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, new RecordingDelay(), input);
        await executor.ExecuteMakro(Macro(new KeyDownBefehl { Key = "not-a-key" }), null!, default);
        Assert.Empty(input.Calls);
    }

    private static Makro Macro(params MakroBefehl[] commands) => new()
    {
        Name = "test",
        Befehle = new ObservableCollection<MakroBefehl>(commands)
    };

    private sealed class RecordingInputController : IInputController
    {
        public List<string> Calls { get; } = [];
        public void MoveAbsolute(double x, double y) => Calls.Add($"absolute:{x}:{y}");
        public void MoveRelative(int deltaX, int deltaY) => Calls.Add($"relative:{deltaX}:{deltaY}");
        public void MouseButton(string button, bool down) => Calls.Add($"mouse:{button}:{down}");
        public void MouseWheel(int deltaX, int deltaY) => Calls.Add($"wheel:{deltaX}:{deltaY}");
        public void Key(VirtualKeyCode key, bool down) => Calls.Add($"key:{key}:{down}");
        public void Text(string text) => Calls.Add($"text:{text}");
    }

    private sealed class RecordingDelay : IPreciseDelayService
    {
        public List<long> Targets { get; } = [];
        public Action? OnDelay { get; init; }
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default)
        {
            Targets.Add(targetTimestamp);
            OnDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
