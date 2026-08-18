namespace TaskAutomation.Makros;

public static class MakroGrouping
{
    public static MakroGroupNormalizationResult Normalize(
        IReadOnlyList<MakroBefehl> commands,
        IReadOnlyList<MakroGruppe> groups)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(groups);

        var uniqueGroups = groups
            .Where(group => !string.IsNullOrWhiteSpace(group.Id))
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .Select(grouping => grouping.First())
            .ToList();
        var validGroupIds = uniqueGroups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command.GroupId) || !validGroupIds.Contains(command.GroupId))
                command.GroupId = null;
        }

        var usedGroupIds = commands
            .Where(command => command.GroupId is not null)
            .Select(command => command.GroupId!)
            .ToHashSet(StringComparer.Ordinal);
        var normalizedGroups = uniqueGroups
            .Where(group => usedGroupIds.Contains(group.Id))
            .ToList();

        var normalizedCommands = new List<MakroBefehl>(commands.Count);
        var emittedGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in commands)
        {
            if (command.GroupId is not { } groupId)
            {
                normalizedCommands.Add(command);
                continue;
            }

            if (!emittedGroupIds.Add(groupId))
                continue;

            normalizedCommands.AddRange(commands.Where(candidate => candidate.GroupId == groupId));
        }

        return new MakroGroupNormalizationResult(normalizedCommands, normalizedGroups);
    }

    public static IReadOnlyList<MakroBefehl> MoveGroupBefore(
        IReadOnlyList<MakroBefehl> commands,
        string groupId,
        MakroBefehl? anchor)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        var moving = commands.Where(step => step.GroupId == groupId).ToList();
        if (moving.Count == 0 || (anchor is not null && moving.Contains(anchor)))
            return commands.ToList();

        var movingSet = moving.ToHashSet();
        var reordered = commands.Where(step => !movingSet.Contains(step)).ToList();
        var insertIndex = anchor is null ? reordered.Count : reordered.IndexOf(anchor);
        if (insertIndex < 0) insertIndex = reordered.Count;
        reordered.InsertRange(insertIndex, moving);
        return reordered;
    }

    public static IReadOnlyList<MakroGruppe> CreateAutomaticMovementGroups(
        IReadOnlyList<MakroBefehl> commands,
        string titlePrefix = "Bewegung")
        => CreateAutomaticGroups(commands,
            command => command is MouseMoveAbsoluteBefehl or MouseMoveRelativeBefehl,
            titlePrefix);

    public static IReadOnlyList<MakroGruppe> CreateAutomaticWheelGroups(
        IReadOnlyList<MakroBefehl> commands,
        string titlePrefix = "Scrollen")
        => CreateAutomaticGroups(commands, command => command is MouseWheelBefehl, titlePrefix);

    private static IReadOnlyList<MakroGruppe> CreateAutomaticGroups(
        IReadOnlyList<MakroBefehl> commands,
        Func<MakroBefehl, bool> matches,
        string titlePrefix)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(matches);
        var groups = new List<MakroGruppe>();
        var runStart = -1;

        for (var index = 0; index <= commands.Count; index++)
        {
            var isMovement = index < commands.Count
                && matches(commands[index])
                && string.IsNullOrWhiteSpace(commands[index].GroupId);

            if (isMovement && runStart < 0)
            {
                runStart = index;
                continue;
            }

            if (isMovement || runStart < 0) continue;

            var count = index - runStart;
            if (count >= 2)
            {
                var group = new MakroGruppe
                {
                    Title = $"{titlePrefix} {groups.Count + 1}",
                    IsAutomatic = true
                };
                groups.Add(group);
                for (var itemIndex = runStart; itemIndex < index; itemIndex++)
                    commands[itemIndex].GroupId = group.Id;
            }
            runStart = -1;
        }

        return groups;
    }
}

public sealed record MakroGroupNormalizationResult(
    IReadOnlyList<MakroBefehl> Commands,
    IReadOnlyList<MakroGruppe> Groups);
