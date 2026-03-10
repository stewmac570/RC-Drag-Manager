using RCDragManagerProd.Controllers;

namespace RCDragManagerProd.Tests.Helpers;

internal sealed class NoOpStandingsDialogService : IStandingsDialogService
{
    public void Show(string title, string content)
    {
        // Intentionally no-op for headless test runs.
    }
}
