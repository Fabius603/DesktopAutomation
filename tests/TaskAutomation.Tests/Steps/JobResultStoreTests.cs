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
        Assert.Same(WindowsStateQueryResult.Default, store.Get<WindowsStateQueryStep, WindowsStateQueryResult>());
        Assert.Same(WindowsStateQueryResult.Default, store.GetById<WindowsStateQueryResult>("missing"));
        Assert.Null(store.GetRaw("missing"));
    }

    [Fact]
    public void Set_IndexesResultByTypeAndIdCaseInsensitively()
    {
        var store = new JobResultStore();
        var result = new WindowsStateQueryResult { WasExecuted = true, Percentage = 42 };
        store.Set<WindowsStateQueryStep>(result, "Audio-Step");
        Assert.Same(result, store.Get<WindowsStateQueryStep, WindowsStateQueryResult>());
        Assert.Same(result, store.GetById<WindowsStateQueryResult>("audio-step"));
        Assert.Same(result, store.GetRaw("AUDIO-STEP"));
    }

    [Fact]
    public void Set_MultipleStepsOfSameType_PreservesEachIdAndLatestType()
    {
        var store = new JobResultStore();
        var first = new WindowsStateQueryResult { Percentage = 10 };
        var second = new WindowsStateQueryResult { Percentage = 90 };
        store.Set<WindowsStateQueryStep>(first, "first");
        store.Set<WindowsStateQueryStep>(second, "second");
        Assert.Same(first, store.GetById<WindowsStateQueryResult>("first"));
        Assert.Same(second, store.GetById<WindowsStateQueryResult>("second"));
        Assert.Same(second, store.Get<WindowsStateQueryStep, WindowsStateQueryResult>());
    }

    [Fact]
    public void RetainOnly_RemovesOtherResults()
    {
        var store = new JobResultStore();
        store.Set<WindowsStateQueryStep>(new WindowsStateQueryResult(), "keep");
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
