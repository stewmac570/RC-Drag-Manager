using System.Collections.Generic;
using RCDragManagerProd.Controllers;

namespace RCDragManagerProd.Tests.Helpers;

/// <summary>
/// Test double for <see cref="IStandingsDialogService"/> that records every
/// <see cref="Show"/> call instead of opening a WinForms dialog. Lets headless tests
/// assert whether (and with what content) the controller tried to surface standings —
/// the reusable counterpart to <see cref="NoOpStandingsDialogService"/> for the cases
/// that need to verify the interaction rather than just suppress it (issue #290).
/// </summary>
internal sealed class RecordingStandingsDialogService : IStandingsDialogService
{
    public List<(string Title, string Content)> ShowCalls { get; } = new();

    public int ShowCount => ShowCalls.Count;

    public (string Title, string Content)? LastShow =>
        ShowCalls.Count == 0 ? null : ShowCalls[ShowCalls.Count - 1];

    public void Show(string title, string content)
    {
        ShowCalls.Add((title, content));
    }
}
