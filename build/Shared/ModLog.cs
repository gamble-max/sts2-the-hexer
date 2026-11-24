using MegaCrit.Sts2.Core.Logging;

namespace Sts2Mods.Shared;

/// <summary>
/// Lightweight wrapper around STS2's native <see cref="Log"/> utility.
/// </summary>
public sealed class ModLog
{
    private readonly string _prefix;

    public ModLog(string modId)
    {
        _prefix = $"[{modId}] ";
    }

    public void Info(string message) => Log.Info(_prefix + message, 2);

    public void Debug(string message) => Log.Debug(_prefix + message, 2);

    public void Warn(string message) => Log.Warn(_prefix + message, 2);

    public void Error(string message) => Log.Error(_prefix + message, 2);
}
