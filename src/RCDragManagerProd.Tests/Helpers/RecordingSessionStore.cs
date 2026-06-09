using RCDragManagerProd.AppServices;

namespace RCDragManagerProd.Tests.Helpers;

/// <summary>
/// Test double for <see cref="IRaceSessionStore"/> that counts persistence calls instead
/// of writing to repositories. Lets headless tests assert that save/close orchestration in
/// <see cref="RaceConsoleService"/> persisted through the store (issue #284).
/// </summary>
internal sealed class RecordingSessionStore : IRaceSessionStore
{
    public int PersistCount { get; private set; }

    public void Persist() => PersistCount++;
}
