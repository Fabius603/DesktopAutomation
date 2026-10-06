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

        for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
        {
            var captured = events[eventIndex];
            if (settings.RecordKeyboard && settings.CombineKeyboardInputs
                && TryCombine(events, eventIndex, out var combined, out var lastIndex))
            {
                Add(combined!, captured.TimestampMicroseconds);
                previousTimestamp = events[lastIndex].TimestampMicroseconds;
                eventIndex = lastIndex;
                continue;
            }
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
                case MouseWheelCaptured wheel:
                    Add(new MouseWheelBefehl { DeltaX = wheel.DeltaX, DeltaY = wheel.DeltaY }, captured.TimestampMicroseconds);
                    break;
            }
        }

        return result;
    }

    private static bool TryCombine(IReadOnlyList<CapturedInputEvent> events, int index, out MakroBefehl? command, out int last)
    {
        command = null;
        last = index;
        if (events[index] is not KeyDownCaptured first) return false;
        if (first.Text is { Length: > 0 } text && index + 1 < events.Count
            && events[index + 1] is KeyUpCaptured release && release.VirtualKey == first.VirtualKey)
        {
            last = index + 1;
            command = new TextInputBefehl { Text = text, DurationMicroseconds = Math.Max(0, events[last].TimestampMicroseconds - first.TimestampMicroseconds) };
            return true;
        }
        var keys = new List<uint>();
        var cursor = index;
        while (cursor < events.Count && events[cursor] is KeyDownCaptured down)
        {
            keys.Add(down.VirtualKey);
            cursor++;
            if (!MakroCommandRules.IsModifier((WindowsInput.Native.VirtualKeyCode)down.VirtualKey)) break;
        }
        var names = keys.Select(key => ((WindowsInput.Native.VirtualKeyCode)key).ToString()).ToList();
        if (!MakroCommandRules.TryParseCombination(names, out _)) return false;
        var released = new HashSet<uint>();
        for (var count = 0; count < keys.Count; count++, cursor++)
        {
            if (cursor >= events.Count || events[cursor] is not KeyUpCaptured up || !keys.Contains(up.VirtualKey) || !released.Add(up.VirtualKey)) return false;
        }
        last = cursor - 1;
        command = new KeyCombinationBefehl { Keys = names, DurationMicroseconds = Math.Max(0, events[last].TimestampMicroseconds - first.TimestampMicroseconds) };
        return true;
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
