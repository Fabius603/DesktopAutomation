namespace TaskAutomation.Steps;

/// <summary>Order-preserving edits for ordinary collections (job control flow has its own owner).</summary>
public static class CollectionSelectionRules
{
    public static bool CanRemove(int total, int selected, int minimum)
        => selected > 0 && selected <= total && total - selected >= minimum;
    public static bool CanDuplicate(int total, int selected, int? maximum)
        => selected > 0 && selected <= total && (!maximum.HasValue || total + selected <= maximum.Value);

    public static IReadOnlyList<int>? MoveOrder(int total, IReadOnlyCollection<int> selected, int direction)
    {
        if (direction is not (-1 or 1) || selected.Count == 0 || selected.Any(index => index < 0 || index >= total)) return null;
        var selection = selected.ToHashSet();
        if (direction < 0 && selection.Contains(0) || direction > 0 && selection.Contains(total - 1)) return null;
        var order = Enumerable.Range(0, total).ToArray();
        foreach (var index in direction < 0 ? selected.Order() : selected.OrderDescending())
            (order[index], order[index + direction]) = (order[index + direction], order[index]);
        return order;
    }
}
