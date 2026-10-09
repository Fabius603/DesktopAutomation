using ImageCapture.DesktopDuplication.RecordingIndicator;
namespace TaskAutomation.Orchestration;

/// <summary>The single recording indicator follows the last active capture owner.</summary>
public sealed class RecordingIndicatorOwnership(IRecordingIndicatorOverlay overlay)
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, RecordingIndicatorOptions> _owners = [];
    public void Show(Guid owner, RecordingIndicatorOptions options)
    {
        lock (_gate)
        {
            _owners.Remove(owner);
            _owners.Add(owner, options);
            overlay.Start(options);
        }
    }
    public void Release(Guid owner)
    {
        lock (_gate)
        {
            if (!_owners.Remove(owner)) return;
            if (_owners.Count == 0) overlay.Stop();
            else overlay.Start(_owners.Last().Value);
        }
    }
}
