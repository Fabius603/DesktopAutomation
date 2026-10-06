using System.Security.Cryptography;
using System.Text;

namespace DesktopAutomation.Application.Deployment;

/// <summary>A cross-process, cross-session file lease, also shared by packaged desktop apps.</summary>
public sealed class ProfileLease : IDisposable
{
    private readonly FileStream _stream;
    private ProfileLease(FileStream stream) => _stream = stream;

    public static ProfileLease? TryAcquire(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            return new ProfileLease(new FileStream(Path.Combine(directory, ".instance.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException exception) when ((exception.HResult & 0xffff) is 32 or 33)
        {
            return null;
        }
    }

    public static string ActivationEventName(string directory) => "Local\\DesktopAutomation.Activate." +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(directory).ToUpperInvariant())));

    public void Dispose() => _stream.Dispose();
}
