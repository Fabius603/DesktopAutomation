namespace TaskAutomation.WindowsIntegration;

public sealed class InputBlockOwnership
{
    private readonly HashSet<Guid> _owners = [];
    public int Count => _owners.Count;
    public void Acquire(Guid owner) => _owners.Add(owner);
    public bool Release(Guid owner) => _owners.Remove(owner) && _owners.Count == 0;
    public void Clear() => _owners.Clear();
}
