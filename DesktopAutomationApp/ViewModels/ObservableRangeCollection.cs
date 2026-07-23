using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DesktopAutomationApp.ViewModels;

/// <summary>
/// Observable collection that publishes one reset notification for a logical
/// range mutation instead of one notification per item.
/// </summary>
internal sealed class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public bool AddRangeBounded(IEnumerable<T> items, int maximumCount)
    {
        CheckReentrancy();
        foreach (var item in items)
            Items.Add(item);

        var removeCount = Math.Max(0, Items.Count - maximumCount);
        for (var index = 0; index < removeCount; index++)
            Items.RemoveAt(0);

        RaiseReset();
        return removeCount > 0;
    }

    public void ReplaceRange(IEnumerable<T> items)
    {
        var replacement = items.ToArray();
        CheckReentrancy();
        Items.Clear();
        foreach (var item in replacement)
            Items.Add(item);
        RaiseReset();
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        var insertion = items.ToArray();
        if (insertion.Length == 0) return;

        CheckReentrancy();
        index = Math.Clamp(index, 0, Items.Count);
        foreach (var item in insertion)
            Items.Insert(index++, item);
        RaiseReset();
    }

    public void RemoveRange(IEnumerable<T> items)
    {
        var removals = items.ToHashSet();
        if (removals.Count == 0) return;

        CheckReentrancy();
        for (var index = Items.Count - 1; index >= 0; index--)
            if (removals.Contains(Items[index]))
                Items.RemoveAt(index);
        RaiseReset();
    }

    private void RaiseReset()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
