using TaskAutomation.Hotkeys;

namespace TaskAutomation.Makros;

public static class MakroRecordingMapper
{
    public static IReadOnlyList<MakroBefehl> Map(
        IReadOnlyList<CapturedInputEvent> source,
        MakroRecordingSettings settings,
        Func<KeyModifiers, uint, string> formatKey,
        Func<MouseButtons, string> formatMouseButton)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(formatKey);
        ArgumentNullException.ThrowIfNull(formatMouseButton);

        var events = settings.RemoveStopGesture ? RemoveStopGesture(source) : source;
        var result = new List<MakroBefehl>(events.Count);
        long previousTimestamp = 0;
        int? previousX = null;
        int? previousY = null;

        void Add(MakroBefehl command, long timestamp)
        {
            command.DelayBeforeMicroseconds = Math.Max(0, timestamp - previousTimestamp);
            previousTimestamp = timestamp;
            result.Add(command);
        }

        void AddMoveTo(int x, int y, long timestamp)
        {
            if (settings.Mode == MakroRecordingMode.ClicksOnly || settings.Mode == MakroRecordingMode.ScreenAccurateAbsolute)
            {
                if (previousX != x || previousY != y)
                    Add(new MouseMoveAbsoluteBefehl { X = x, Y = y }, timestamp);
            }
            else if (previousX.HasValue)
            {
                var deltaX = x - previousX.Value;
                var deltaY = y - previousY!.Value;
                if (deltaX != 0 || deltaY != 0)
                    Add(new MouseMoveRelativeBefehl { DeltaX = deltaX, DeltaY = deltaY }, timestamp);
            }
            previousX = x;
            previousY = y;
        }

        foreach (var captured in events)
        {
            switch (captured)
            {
                case KeyDownCaptured key when settings.RecordKeyboard:
                    Add(new KeyDownBefehl { Key = formatKey(KeyModifiers.None, key.VirtualKey) }, captured.TimestampMicroseconds);
                    break;
                case KeyUpCaptured key when settings.RecordKeyboard:
                    Add(new KeyUpBefehl { Key = formatKey(KeyModifiers.None, key.VirtualKey) }, captured.TimestampMicroseconds);
                    break;
                case MouseMoveCaptured move when settings.Mode != MakroRecordingMode.ClicksOnly:
                    AddMoveTo(move.X, move.Y, captured.TimestampMicroseconds);
                    break;
                case MouseMoveCaptured move:
                    previousX = move.X;
                    previousY = move.Y;
                    break;
                case MouseDownCaptured mouse when settings.RecordMouseButtons:
                    AddMoveTo(mouse.X, mouse.Y, captured.TimestampMicroseconds);
                    Add(new MouseDownBefehl { Button = formatMouseButton(mouse.Button) }, captured.TimestampMicroseconds);
                    break;
                case MouseUpCaptured mouse when settings.RecordMouseButtons:
                    AddMoveTo(mouse.X, mouse.Y, captured.TimestampMicroseconds);
                    Add(new MouseUpBefehl { Button = formatMouseButton(mouse.Button) }, captured.TimestampMicroseconds);
                    break;
            }
        }

        return result;
    }

    private static IReadOnlyList<CapturedInputEvent> RemoveStopGesture(IReadOnlyList<CapturedInputEvent> events)
    {
        var lastMouseDown = -1;
        for (var index = events.Count - 1; index >= 0; index--)
        {
            if (events[index] is MouseDownCaptured)
            {
                lastMouseDown = index;
                break;
            }
        }

        if (lastMouseDown < 0)
            return events;

        var hasMatchingMouseUp = events.Skip(lastMouseDown + 1).Any(item => item is MouseUpCaptured);
        return hasMatchingMouseUp ? events.Take(lastMouseDown).ToArray() : events;
    }
}
