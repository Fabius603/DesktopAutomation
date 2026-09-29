using System.Collections.Specialized;
using DesktopAutomationApp.ViewModels;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class ObservableRangeCollectionTests
{
    [Fact]
    public void ReplaceRange_RaisesSingleReset()
    {
        var collection = new ObservableRangeCollection<int>();
        var notifications = new List<NotifyCollectionChangedEventArgs>();
        collection.CollectionChanged += (_, args) => notifications.Add(args);

        collection.ReplaceRange(Enumerable.Range(1, 500));

        Assert.Equal(500, collection.Count);
        var notification = Assert.Single(notifications);
        Assert.Equal(NotifyCollectionChangedAction.Reset, notification.Action);
    }

    [Fact]
    public void InsertRange_RaisesSingleResetAndPreservesOrder()
    {
        var collection = new ObservableRangeCollection<int>();
        collection.ReplaceRange([1, 4]);
        var notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;

        collection.InsertRange(1, [2, 3]);

        Assert.Equal([1, 2, 3, 4], collection);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void RemoveRange_RaisesSingleReset()
    {
        var collection = new ObservableRangeCollection<int>();
        collection.ReplaceRange(Enumerable.Range(1, 10));
        var notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;

        collection.RemoveRange([2, 4, 6, 8]);

        Assert.Equal([1, 3, 5, 7, 9, 10], collection);
        Assert.Equal(1, notifications);
    }
}
