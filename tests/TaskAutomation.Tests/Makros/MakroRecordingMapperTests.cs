using TaskAutomation.Hotkeys;
using TaskAutomation.Makros;

namespace TaskAutomation.Tests.Makros;

public sealed class MakroRecordingMapperTests
{
    [Fact]
    public void Map_ScreenAccurateCreatesAbsoluteMovesWithMicrosecondTiming()
    {
        CapturedInputEvent[] events =
        [
            new MouseMoveCaptured(100, 200) { TimestampMicroseconds = 0 },
            new MouseMoveCaptured(104, 203) { TimestampMicroseconds = 1_250 },
            new MouseMoveCaptured(110, 208) { TimestampMicroseconds = 2_900 }
        ];
        var result = MakroRecordingMapper.Map(events, Settings(MakroRecordingMode.ScreenAccurateAbsolute), FormatKey, FormatButton);
        Assert.Collection(result,
            item => AssertMove(item, 100, 200, 0),
            item => AssertMove(item, 104, 203, 1_250),
            item => AssertMove(item, 110, 208, 1_650));
    }

    [Fact]
    public void Map_MotionFaithfulCreatesDeltasAndKeepsTimingFromRecordingStart()
    {
        CapturedInputEvent[] events =
        [
            new MouseMoveCaptured(100, 200) { TimestampMicroseconds = 0 },
            new MouseMoveCaptured(104, 198) { TimestampMicroseconds = 800 },
            new MouseMoveCaptured(110, 203) { TimestampMicroseconds = 2_000 }
        ];
        var result = MakroRecordingMapper.Map(events, Settings(MakroRecordingMode.MotionFaithfulRelative), FormatKey, FormatButton);
        Assert.Collection(result,
            item => AssertRelative(item, 4, -2, 800),
            item => AssertRelative(item, 6, 5, 1_200));
    }

    [Fact]
    public void Map_ClicksOnlyMovesExactlyToClickAndCanRecordXButtons()
    {
        CapturedInputEvent[] events =
        [
            new MouseMoveCaptured(10, 20) { TimestampMicroseconds = 0 },
            new MouseDownCaptured(MouseButtons.X1, 40, 50) { TimestampMicroseconds = 5_000 },
            new MouseUpCaptured(MouseButtons.X1, 40, 50) { TimestampMicroseconds = 7_000 }
        ];
        var result = MakroRecordingMapper.Map(events, Settings(MakroRecordingMode.ClicksOnly), FormatKey, FormatButton);
        Assert.Collection(result,
            item => AssertMove(item, 40, 50, 5_000),
            item => { var down = Assert.IsType<MouseDownBefehl>(item); Assert.Equal("X1", down.Button); Assert.Equal(0, down.DelayBeforeMicroseconds); },
            item => { var up = Assert.IsType<MouseUpBefehl>(item); Assert.Equal("X1", up.Button); Assert.Equal(2_000, up.DelayBeforeMicroseconds); });
    }

    [Fact]
    public void Map_RemoveStopGestureOnlyRemovesFinalCompleteClickGesture()
    {
        CapturedInputEvent[] events =
        [
            new KeyDownCaptured(65) { TimestampMicroseconds = 1_000 },
            new MouseDownCaptured(MouseButtons.Left, 5, 6) { TimestampMicroseconds = 2_000 },
            new MouseUpCaptured(MouseButtons.Left, 5, 6) { TimestampMicroseconds = 2_100 }
        ];
        var settings = Settings(MakroRecordingMode.ClicksOnly);
        settings.RemoveStopGesture = true;
        var result = MakroRecordingMapper.Map(events, settings, FormatKey, FormatButton);
        Assert.Single(result);
        Assert.IsType<KeyDownBefehl>(result[0]);
    }

    [Fact]
    public void Map_DisabledInputKindsAreOmitted()
    {
        CapturedInputEvent[] events =
        [
            new KeyDownCaptured(65) { TimestampMicroseconds = 100 },
            new MouseDownCaptured(MouseButtons.Left, 1, 1) { TimestampMicroseconds = 200 }
        ];
        var settings = Settings(MakroRecordingMode.ClicksOnly);
        settings.RecordKeyboard = false;
        settings.RecordMouseButtons = false;
        Assert.Empty(MakroRecordingMapper.Map(events, settings, FormatKey, FormatButton));
    }

    private static MakroRecordingSettings Settings(MakroRecordingMode mode) => new() { Mode = mode, RemoveStopGesture = false };
    private static string FormatKey(KeyModifiers _, uint key) => key.ToString();
    private static string FormatButton(MouseButtons button) => button.ToString();
    private static void AssertMove(MakroBefehl item, int x, int y, long delay)
    {
        var move = Assert.IsType<MouseMoveAbsoluteBefehl>(item);
        Assert.Equal((x, y, delay), (move.X, move.Y, move.DelayBeforeMicroseconds));
    }
    private static void AssertRelative(MakroBefehl item, int x, int y, long delay)
    {
        var move = Assert.IsType<MouseMoveRelativeBefehl>(item);
        Assert.Equal((x, y, delay), (move.DeltaX, move.DeltaY, move.DelayBeforeMicroseconds));
    }
}
