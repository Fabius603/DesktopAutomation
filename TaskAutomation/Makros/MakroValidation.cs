namespace TaskAutomation.Makros;

public enum MakroValidationError
{
    None,
    NameRequired,
    CommandRequired,
    MouseButtonInvalid,
    KeyRequired,
    DurationInvalid,
    RecordingSettingsInvalid,
    CommandTimingInvalid,
    UnknownCommand
}

public sealed record MakroCommandValidationResult(MakroBefehl Command, bool IsValid, MakroValidationError Error);
public sealed record MakroValidationResult(bool IsValid, MakroValidationError Error, IReadOnlyList<MakroCommandValidationResult> Commands);

public static class MakroValidation
{
    public static string Describe(MakroValidationError error) => error switch
    {
        MakroValidationError.NameRequired => "Der Name ist erforderlich.",
        MakroValidationError.CommandRequired => "Das Makro muss mindestens einen Befehl enthalten.",
        MakroValidationError.MouseButtonInvalid => "Die Maustaste ist ungueltig.",
        MakroValidationError.KeyRequired => "Es wurde keine Taste ausgewaehlt.",
        MakroValidationError.DurationInvalid => "Die Dauer darf nicht negativ sein.",
        MakroValidationError.RecordingSettingsInvalid => "Die Aufnahmeeinstellungen sind ungueltig.",
        MakroValidationError.CommandTimingInvalid => "Die Befehlsverzoegerung darf nicht negativ sein.",
        MakroValidationError.UnknownCommand => "Das Makro enthaelt einen unbekannten Befehl.",
        _ => string.Empty
    };
    public static bool CanConfirm(MakroBefehl? command) => command != null && ValidateCommand(command).IsValid;
    public static bool IsCommandAllowed(MakroBefehl command) => ValidateCommand(command).IsValid;
    public static bool IsMakroAllowed(Makro makro) => Validate(makro).IsValid;

    public static MakroValidationResult Validate(Makro makro)
    {
        var commands = makro.Befehle.Select(ValidateCommand).ToList();
        var settingsValid = makro.RecordingSettings.MinimumIntervalMicroseconds is >= 0 and <= 1_000_000
                            && makro.RecordingSettings.MinimumDistancePixels is >= 0 and <= 10_000
                            && makro.RecordingSettings.RecordingHotkeyVirtualKey is > 0 and not 0x79;
        var error = string.IsNullOrWhiteSpace(makro.Name)
            ? MakroValidationError.NameRequired
            : makro.Befehle.Count == 0
                ? MakroValidationError.CommandRequired
            : !settingsValid
                ? MakroValidationError.RecordingSettingsInvalid
            : commands.FirstOrDefault(r => !r.IsValid)?.Error ?? MakroValidationError.None;
        return new(error == MakroValidationError.None, error, commands);
    }

    public static MakroCommandValidationResult ValidateCommand(MakroBefehl command)
    {
        var error = command.DelayBeforeMicroseconds < 0
            ? MakroValidationError.CommandTimingInvalid
            : command switch
        {
            MouseMoveAbsoluteBefehl => MakroValidationError.None,
            MouseMoveRelativeBefehl => MakroValidationError.None,
            MouseDownBefehl s when IsMouseButton(s.Button) => MakroValidationError.None,
            MouseUpBefehl s when IsMouseButton(s.Button) => MakroValidationError.None,
            MouseDownBefehl or MouseUpBefehl => MakroValidationError.MouseButtonInvalid,
            KeyDownBefehl s when !string.IsNullOrWhiteSpace(s.Key) => MakroValidationError.None,
            KeyUpBefehl s when !string.IsNullOrWhiteSpace(s.Key) => MakroValidationError.None,
            KeyDownBefehl or KeyUpBefehl => MakroValidationError.KeyRequired,
            TimeoutBefehl s when s.Duration >= 0 => MakroValidationError.None,
            TimeoutBefehl => MakroValidationError.DurationInvalid,
            _ => MakroValidationError.UnknownCommand
        };
        return new(command, error == MakroValidationError.None, error);
    }

    private static bool IsMouseButton(string? button)
        => button?.ToLowerInvariant() is "left" or "right" or "middle" or "x1" or "x2";
}
