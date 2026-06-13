using System.Windows;

namespace RCDragManagerProd.WPF
{
    /// <summary>
    /// Keeps windows within the screen's work area so docked footers (action
    /// buttons) stay visible on smaller displays. Call FitToScreen after
    /// InitializeComponent.
    /// </summary>
    public static class WindowSizing
    {
        public static void FitToScreen(Window w)
        {
            void Apply()
            {
                var wa = SystemParameters.WorkArea;

                // Cap so it can never exceed the work area (incl. later resizes).
                w.MaxWidth = wa.Width;
                w.MaxHeight = wa.Height;

                if (w.Width > wa.Width) w.Width = wa.Width;
                if (w.Height > wa.Height) w.Height = wa.Height;

                // Re-centre within the work area in case clamping moved it off-screen.
                w.Left = wa.Left + (wa.Width - w.Width) / 2;
                w.Top = wa.Top + (wa.Height - w.Height) / 2;
            }

            if (w.IsLoaded) Apply();
            else w.SourceInitialized += (_, __) => Apply();
        }
    }
}
