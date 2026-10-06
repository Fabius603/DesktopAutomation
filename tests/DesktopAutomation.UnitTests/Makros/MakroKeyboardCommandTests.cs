using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Hotkeys;
using TaskAutomation.Makros;
using TaskAutomation.Timing;
using WindowsInput.Native;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroKeyboardCommandTests
{
    [Fact]
    public async Task TextInput_PreservesUnicodeAndSupplementaryCharacters()
    {
        var input = new Input();
        await Executor(input).ExecuteMakro(Macro(new TextInputBefehl { Text = "Grüße 漢字 😀\n" }), null!, default);
        Assert.Equal("Grüße 漢字 😀\n", string.Concat(input.Texts));
        Assert.Empty(input.Keys);
    }

    [Fact]
    public async Task Combination_ReleasesOnlyItsOwnKeysInReverseOrder()
    {
        var input = new Input();
        await Executor(input).ExecuteMakro(Macro(new KeyDownBefehl { Key = "CONTROL" },
            new KeyCombinationBefehl { Keys = ["Ctrl", "S"] }, new KeyUpBefehl { Key = "CONTROL" }), null!, default);
        Assert.Equal([(VirtualKeyCode.CONTROL, true), (VirtualKeyCode.VK_S, true), (VirtualKeyCode.VK_S, false), (VirtualKeyCode.CONTROL, false)], input.Keys);
    }

    [Fact]
    public async Task Combination_CancellationReleasesMainKeyAndModifiers()
    {
        using var cancellation = new CancellationTokenSource();
        var input = new Input();
        var executor = new MakroExecutor(NullLogger<MakroExecutor>.Instance, new Delay(() => cancellation.Cancel()), input);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteMakro(Macro(
            new KeyCombinationBefehl { Keys = ["Ctrl", "Shift", "S"], DurationMicroseconds = 500 }), null!, cancellation.Token));
        Assert.Equal([(VirtualKeyCode.CONTROL, true), (VirtualKeyCode.SHIFT, true), (VirtualKeyCode.VK_S, true),
            (VirtualKeyCode.VK_S, false), (VirtualKeyCode.SHIFT, false), (VirtualKeyCode.CONTROL, false)], input.Keys);
    }

    [Fact]
    public async Task Combination_InputFailureStillReleasesHeldKeys()
    {
        var input = new Input { FailOn = VirtualKeyCode.VK_S };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Executor(input).ExecuteMakro(Macro(
            new KeyCombinationBefehl { Keys = ["Ctrl", "S"] }), null!, default));
        Assert.Contains((VirtualKeyCode.CONTROL, false), input.Keys);
    }

    [Theory]
    [InlineData("Ctrl+S", true)]
    [InlineData("Ctrl+Shift+S", true)]
    [InlineData("Ctrl", false)]
    [InlineData("Ctrl+unknown", false)]
    [InlineData("Ctrl+Ctrl+S", false)]
    [InlineData("S+Ctrl", false)]
    public void CombinationValidation_RejectsAmbiguousOrInvalidKeys(string value, bool valid)
        => Assert.Equal(valid, MakroValidation.ValidateCommand(new KeyCombinationBefehl { Keys = value.Split('+').ToList() }).IsValid);

    [Fact]
    public void TextValidation_RejectsEmptyTextAndNegativeRecordedDuration()
    {
        Assert.False(MakroValidation.ValidateCommand(new TextInputBefehl()).IsValid);
        Assert.False(MakroValidation.ValidateCommand(new TextInputBefehl { Text = "A", DurationMicroseconds = -1 }).IsValid);
        Assert.True(MakroValidation.ValidateCommand(new TextInputBefehl { Text = " " }).IsValid);
    }

    [Fact]
    public void Timeline_IncludesCompositeDurationAndDelayBeforeFollowingStep()
    {
        var commands = new MakroBefehl[] { new TextInputBefehl { Text = "a", DelayBeforeMicroseconds = 100, DurationMicroseconds = 200 },
            new KeyCombinationBefehl { Keys = ["Ctrl", "S"], DelayBeforeMicroseconds = 50, DurationMicroseconds = 150 }, new TimeoutBefehl { Duration = 1 } };
        Assert.Equal([100L, 350L, 500L], MakroTimeline.Calculate(commands).Select(entry => entry.ExecutionTimeMicroseconds));
        Assert.Equal(1500, MakroTimeline.GetTotalDurationMicroseconds(commands));
    }

    [Fact]
    public void Recording_CombinesRecognizedTextAndRetainsFollowingTiming()
    {
        CapturedInputEvent[] events = [new KeyDownCaptured(0x41) { Text = "ä", TimestampMicroseconds = 100 },
            new KeyUpCaptured(0x41) { TimestampMicroseconds = 300 }, new MouseWheelCaptured(0, 120) { TimestampMicroseconds = 450 }];
        var commands = Map(events, true);
        var text = Assert.IsType<TextInputBefehl>(commands[0]);
        Assert.Equal(("ä", 100L, 200L), (text.Text, text.DelayBeforeMicroseconds!.Value, text.DurationMicroseconds));
        Assert.Equal(150, commands[1].DelayBeforeMicroseconds);
        Assert.Equal(450, MakroTimeline.Calculate(commands)[1].ExecutionTimeMicroseconds);
    }

    [Fact]
    public void Recording_CombinesOnlyCompleteUninterruptedChords()
    {
        CapturedInputEvent[] events = [new KeyDownCaptured(0x11) { TimestampMicroseconds = 100 },
            new KeyDownCaptured(0x53) { TimestampMicroseconds = 200 }, new KeyUpCaptured(0x53) { TimestampMicroseconds = 300 },
            new KeyUpCaptured(0x11) { TimestampMicroseconds = 400 }];
        var chord = Assert.IsType<KeyCombinationBefehl>(Assert.Single(Map(events, true)));
        Assert.Equal(["CONTROL", "VK_S"], chord.Keys);
        Assert.Equal(300, chord.DurationMicroseconds);
        Assert.Equal(4, Map(events, false).Count);
        Assert.Equal(3, Map(events[..3], true).Count);
        Assert.Equal(5, Map([events[0], events[1], new MouseWheelCaptured(0, 120), events[2], events[3]], true).Count);
    }

    [Fact]
    public void Recording_UnknownTextAndDisabledKeyboardPreserveOriginalPolicy()
    {
        CapturedInputEvent[] events = [new KeyDownCaptured(0x41), new KeyUpCaptured(0x41)];
        Assert.IsType<KeyDownBefehl>(Map(events, true)[0]);
        var commands = MakroRecordingMapper.Map(events, new() { RecordKeyboard = false, CombineKeyboardInputs = true }, (_, key) => ((VirtualKeyCode)key).ToString(), button => button.ToString());
        Assert.Empty(commands);
    }

    private static IReadOnlyList<MakroBefehl> Map(CapturedInputEvent[] events, bool combine) => MakroRecordingMapper.Map(events,
        new() { CombineKeyboardInputs = combine, RemoveStopGesture = false }, (_, key) => ((VirtualKeyCode)key).ToString(), button => button.ToString());
    private static Makro Macro(params MakroBefehl[] commands) => new() { Name = "test", Befehle = new ObservableCollection<MakroBefehl>(commands) };
    private static MakroExecutor Executor(Input input) => new(NullLogger<MakroExecutor>.Instance, new Delay(), input);
    private sealed class Input : IInputController
    {
        public List<string> Texts { get; } = [];
        public List<(VirtualKeyCode, bool)> Keys { get; } = [];
        public VirtualKeyCode? FailOn { get; init; }
        public void Text(string text) => Texts.Add(text);
        public void Key(VirtualKeyCode key, bool down)
        {
            Keys.Add((key, down));
            if (down && key == FailOn) throw new InvalidOperationException("Simulated input failure.");
        }
        public void MoveAbsolute(double x, double y) { }
        public void MoveRelative(int deltaX, int deltaY) { }
        public void MouseButton(string button, bool down) { }
        public void MouseWheel(int deltaX, int deltaY) { }
    }
    private sealed class Delay(Action? onDelay = null) : IPreciseDelayService
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DelayUntilAsync(long timestamp, CancellationToken cancellationToken = default)
        {
            onDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
