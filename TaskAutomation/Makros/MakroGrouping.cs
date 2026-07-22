namespace TaskAutomation.Makros;

public static class MakroGrouping
{
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
    {
        ArgumentNullException.ThrowIfNull(commands);
        var groups = new List<MakroGruppe>();
        var runStart = -1;

        for (var index = 0; index <= commands.Count; index++)
        {
            var isMovement = index < commands.Count
                && commands[index] is MouseMoveAbsoluteBefehl or MouseMoveRelativeBefehl
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
