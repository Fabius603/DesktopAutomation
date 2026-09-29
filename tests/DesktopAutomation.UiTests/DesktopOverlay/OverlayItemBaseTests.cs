using DesktopOverlay;
using GameOverlay.Drawing;

namespace TaskAutomation.Tests.DesktopOverlay;

public sealed class OverlayItemBaseTests
{
    [Fact]
    public void InvalidatedGraphicsResourcesAreSetUpAgain()
    {
        var item = new RecoverableOverlayItem();

        item.Setup(null!, recreate: false);
        Assert.True(item.IsSetup);

        item.SimulateGraphicsFailure();
        Assert.False(item.IsSetup);

        item.Setup(null!, recreate: false);
        Assert.True(item.IsSetup);
        Assert.Equal(2, item.SetupCount);
    }

    private sealed class RecoverableOverlayItem() : OverlayItemBase("test")
    {
        public int SetupCount { get; private set; }

        public override void Setup(Graphics gfx, bool recreate)
        {
            base.Setup(gfx, recreate);
            SetupCount++;
        }

        public void SimulateGraphicsFailure() => InvalidateSetup();
        public override void Draw(Graphics gfx) { }
        public override void Dispose() { }
    }
}
