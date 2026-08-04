namespace TaskAutomation.Contracts.Steps;

public static class StepEditorActivity
{
    public static IReadOnlySet<string> GetActiveFieldIds(
        StepDescriptor descriptor,
        Func<string, string?> selectionValueResolver,
        Func<string, bool>? fieldVisibilityResolver = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(selectionValueResolver);

        var active = descriptor.Fields.Select(field => field.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var section in descriptor.Presentation.EditorSections)
        {
            foreach (var node in section.EditorNodes ?? [])
            {
                if (node is not StepChoiceGroupDescriptor group)
                    continue;
                foreach (var branch in group.Branches)
                    foreach (var child in branch.Children)
                        RemoveTree(child);
                ActivateSelectedBranch(group);
            }
        }
        return active;

        void RemoveTree(StepEditorNodeDescriptor node)
        {
            switch (node)
            {
                case StepFieldNodeDescriptor field:
                    active.Remove(field.FieldId);
                    break;
                case StepChoiceGroupDescriptor group:
                    active.Remove(group.SelectionFieldId);
                    foreach (var branch in group.Branches)
                        foreach (var child in branch.Children)
                            RemoveTree(child);
                    break;
            }
        }

        void ActivateNode(StepEditorNodeDescriptor node)
        {
            switch (node)
            {
                case StepFieldNodeDescriptor field:
                    active.Add(field.FieldId);
                    break;
                case StepChoiceGroupDescriptor group:
                    active.Add(group.SelectionFieldId);
                    ActivateSelectedBranch(group);
                    break;
            }
        }

        void ActivateSelectedBranch(StepChoiceGroupDescriptor group)
        {
            if (fieldVisibilityResolver?.Invoke(group.SelectionFieldId) == false)
                return;
            var selectedValue = selectionValueResolver(group.SelectionFieldId);
            var branch = group.Branches.FirstOrDefault(candidate =>
                string.Equals(candidate.Value, selectedValue, StringComparison.Ordinal));
            if (branch is null)
                return;
            foreach (var child in branch.Children)
                ActivateNode(child);
        }
    }
}
