using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class JobResultStoreTests
{
    [Fact]
    public void Get_WhenNothingStored_ReturnsSharedDefault()
    {
        var store = new JobResultStore();
        var byType = store.Get<WindowsStateQueryStep, AudioVolumeQueryResult>();
        var byId = store.GetById<AudioVolumeQueryResult>("missing");
        Assert.Same(byType, byId);
        Assert.False(byType.WasExecuted);
        Assert.Null(store.GetRaw("missing"));
    }

    [Fact]
    public void Set_IndexesResultByTypeAndIdCaseInsensitively()
    {
        var store = new JobResultStore();
        var result = new AudioVolumeQueryResult { WasExecuted = true, Percentage = 42 };
        store.Set<WindowsStateQueryStep>(result, "Audio-Step");
        Assert.Same(result, store.Get<WindowsStateQueryStep, AudioVolumeQueryResult>());
        Assert.Same(result, store.GetById<AudioVolumeQueryResult>("audio-step"));
        Assert.Same(result, store.GetRaw("AUDIO-STEP"));
    }

    [Fact]
    public void Set_MultipleStepsOfSameType_PreservesEachIdAndLatestType()
    {
        var store = new JobResultStore();
        var first = new AudioVolumeQueryResult { Percentage = 10 };
        var second = new AudioVolumeQueryResult { Percentage = 90 };
        store.Set<WindowsStateQueryStep>(first, "first");
        store.Set<WindowsStateQueryStep>(second, "second");
        Assert.Same(first, store.GetById<AudioVolumeQueryResult>("first"));
        Assert.Same(second, store.GetById<AudioVolumeQueryResult>("second"));
        Assert.Same(second, store.Get<WindowsStateQueryStep, AudioVolumeQueryResult>());
    }

    [Fact]
    public void RetainOnly_RemovesOtherResults()
    {
        var store = new JobResultStore();
        store.Set<WindowsStateQueryStep>(new AudioVolumeQueryResult(), "keep");
        store.Set<ShowTextStep>(new ShowTextResult(), "remove");
        store.RetainOnly(["KEEP"]);
        Assert.NotNull(store.GetRaw("keep"));
        Assert.Null(store.GetRaw("remove"));
    }

    [Fact]
    public void DisposeAndClear_DisposesStoredBitmapAndClearsIndexes()
    {
        var bitmap = new Bitmap(2, 2);
        var store = new JobResultStore();
        store.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = bitmap }, "capture");
        store.DisposeAndClear();
        Assert.Null(store.GetRaw("capture"));
        Assert.Throws<ArgumentException>(() => bitmap.GetHbitmap());
    }
}
