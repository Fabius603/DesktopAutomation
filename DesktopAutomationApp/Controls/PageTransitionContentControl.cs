using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DesktopAutomationApp.Controls;

/// <summary>
/// Shows newly navigated content only after its first layout pass and then uses
/// compositor-friendly properties for a short page transition.
/// </summary>
public sealed class PageTransitionContentControl : ContentControl
{
    private static readonly Duration TransitionDuration = new(TimeSpan.FromMilliseconds(180));
    private readonly TranslateTransform _translation = new();
    private long _transitionVersion;

    public PageTransitionContentControl()
    {
        RenderTransform = _translation;
        RenderTransformOrigin = new Point(0.5, 0.5);
        ClipToBounds = true;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        var version = ++_transitionVersion;
        BeginAnimation(OpacityProperty, null);
        _translation.BeginAnimation(TranslateTransform.YProperty, null);
        Opacity = 0;
        _translation.Y = 8;

        // Render priority lets DataGrid resolve its star-sized columns before it
        // becomes visible, preventing the brief collapsed-table frame.
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            if (version != _transitionVersion)
                return;

            UpdateLayout();
            CacheMode = new BitmapCache();

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var fade = new DoubleAnimation(0, 1, TransitionDuration) { EasingFunction = easing };
            var slide = new DoubleAnimation(8, 0, TransitionDuration) { EasingFunction = easing };

            fade.Completed += (_, _) =>
            {
                if (version == _transitionVersion)
                    CacheMode = null;
            };

            BeginAnimation(OpacityProperty, fade, HandoffBehavior.SnapshotAndReplace);
            _translation.BeginAnimation(
                TranslateTransform.YProperty,
                slide,
                HandoffBehavior.SnapshotAndReplace);
        }));
    }
}
