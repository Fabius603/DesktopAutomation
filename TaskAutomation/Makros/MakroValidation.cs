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
    GroupStructureInvalid,
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
        MakroValidationError.GroupStructureInvalid => "Die Makrogruppen sind ungueltig.",
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
                            && makro.RecordingSettings.RecordingHotkeyVirtualKey > 0;
        var groupsValid = ValidateGroups(makro);
        var error = string.IsNullOrWhiteSpace(makro.Name)
            ? MakroValidationError.NameRequired
            : makro.Befehle.Count == 0
                ? MakroValidationError.CommandRequired
            : !settingsValid
                ? MakroValidationError.RecordingSettingsInvalid
            : !groupsValid
                ? MakroValidationError.GroupStructureInvalid
            : commands.FirstOrDefault(r => !r.IsValid)?.Error ?? MakroValidationError.None;
        return new(error == MakroValidationError.None, error, commands);
    }

    private static bool ValidateGroups(Makro makro)
    {
        if (makro.Gruppen.Any(group => string.IsNullOrWhiteSpace(group.Id)))
            return false;

        var groupIds = makro.Gruppen.Select(group => group.Id).ToList();
        if (groupIds.Distinct(StringComparer.Ordinal).Count() != groupIds.Count)
            return false;

        var knownGroupIds = groupIds.ToHashSet(StringComparer.Ordinal);
        if (makro.Befehle.Any(command => command.GroupId is { } id && !knownGroupIds.Contains(id)))
            return false;
        if (knownGroupIds.Any(id => makro.Befehle.All(command => command.GroupId != id)))
            return false;

        var completedGroups = new HashSet<string>(StringComparer.Ordinal);
        string? currentGroupId = null;
        foreach (var command in makro.Befehle)
        {
            if (command.GroupId == currentGroupId)
                continue;
            if (currentGroupId is not null)
                completedGroups.Add(currentGroupId);
            currentGroupId = command.GroupId;
            if (currentGroupId is not null && completedGroups.Contains(currentGroupId))
                return false;
        }

        return true;
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
