using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RCDragManagerProd.WPF
{
    /// <summary>
    /// Keeps windows within the screen's work area so docked footers (action
    /// buttons) stay visible — both for normal sizing and when a borderless
    /// (WindowStyle=None) window is maximized (which otherwise overflows past the
    /// taskbar). Call FitToScreen after InitializeComponent.
    ///
    /// <para><b>Window sizing standard (#420/#421).</b> The minimum supported
    /// display is the race-day laptop: 1920x1080 at 150% scaling = 1280x720 DIU,
    /// or 1280x688 usable once the taskbar is subtracted. Every window must be
    /// designed to work at that size, not merely clamped into it.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Workspace windows</b> — Landing, Setup, Load, Driver Manager,
    /// Race Console, Multi-Class — open at 1180x660 (min 980x620) so navigating
    /// between them never changes the window footprint.</item>
    /// <item><b>Utility windows</b> — Driver Stats, Settings, Live Scoreboard —
    /// share a 600 width; height may be content-sized.</item>
    /// <item>Anything that docks a footer of buttons calls
    /// <see cref="FitToScreen"/>, never <see cref="RoundCorners"/> alone.</item>
    /// </list>
    ///
    /// <para><b>Dialog tiers (race-day feedback, Oct 2026).</b> Every dialog is one of
    /// three sizes, can be resized, and is kept on screen:</para>
    /// <list type="bullet">
    /// <item><b>Small</b> (<see cref="SmallDialogWidth"/> wide, height fits content) —
    /// prompts and short forms: add/edit driver or car, dial-in, qual time, edit
    /// result, reset class, message. Content scrolls if the screen is too short.</item>
    /// <item><b>Medium</b> (<see cref="MediumDialogWidth"/> x
    /// <see cref="MediumDialogHeight"/>) — a single list: buybacks, pick a result to
    /// edit, text summary.</item>
    /// <item><b>Large</b> (<see cref="LargeDialogWidth"/> x
    /// <see cref="LargeDialogHeight"/>) — grids and multi-pane screens: class setup,
    /// race roster, results, class and event completion.</item>
    /// </list>
    /// <para>Large dialogs call <see cref="FitToScreen"/>; small and medium call
    /// <see cref="FitDialogToScreen"/>, which keeps them beside their owner.
    /// DialogSizingStandardTests enforces this from the XAML.</para>
    ///
    /// <para>WindowSizingStandardTests in the test project enforces the numbers
    /// by reading the XAML, so new windows are caught before review.</para>
    /// </summary>
    public static class WindowSizing
    {
        public const double SmallDialogWidth = 440;
        public const double MediumDialogWidth = 600;
        public const double MediumDialogHeight = 560;
        public const double LargeDialogWidth = 1040;
        public const double LargeDialogHeight = 660;

        /// <summary>
        /// For small and medium dialogs. Caps the dialog at the screen's work area and,
        /// once it is shown or resized, nudges it back so no edge sits off screen or under
        /// the taskbar. Unlike <see cref="FitToScreen"/> it leaves the dialog where
        /// WindowStartupLocation put it (usually centred on its owner).
        /// </summary>
        public static void FitDialogToScreen(Window w)
        {
            void Constrain()
            {
                var wa = SystemParameters.WorkArea;
                w.MaxWidth = wa.Width;
                w.MaxHeight = wa.Height;
            }

            void KeepOnScreen()
            {
                var wa = SystemParameters.WorkArea;
                double width = w.ActualWidth, height = w.ActualHeight;
                if (double.IsNaN(w.Left) || double.IsNaN(w.Top) || width <= 0 || height <= 0) return;
                if (w.Left + width > wa.Right) w.Left = Math.Max(wa.Left, wa.Right - width);
                if (w.Top + height > wa.Bottom) w.Top = Math.Max(wa.Top, wa.Bottom - height);
                if (w.Left < wa.Left) w.Left = wa.Left;
                if (w.Top < wa.Top) w.Top = wa.Top;
            }

            void Apply()
            {
                Constrain();
                var hwnd = new WindowInteropHelper(w).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
                RoundCornersHwnd(hwnd);
            }

            if (w.IsLoaded) Apply();
            else w.SourceInitialized += (_, __) => Apply();
            w.Loaded += (_, __) => KeepOnScreen();
            w.SizeChanged += (_, __) => KeepOnScreen();
        }

        public static void FitToScreen(Window w)
        {
            void Apply()
            {
                var wa = SystemParameters.WorkArea;
                w.MaxWidth = wa.Width;
                w.MaxHeight = wa.Height;
                if (w.Width > wa.Width) w.Width = wa.Width;
                if (w.Height > wa.Height) w.Height = wa.Height;
                w.Left = wa.Left + (wa.Width - w.Width) / 2;
                w.Top = wa.Top + (wa.Height - w.Height) / 2;

                var hwnd = new WindowInteropHelper(w).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
                RoundCornersHwnd(hwnd);
            }

            if (w.IsLoaded) Apply();
            else w.SourceInitialized += (_, __) => Apply();
        }

        /// <summary>Rounds the window corners (and lets DWM cast its drop shadow) on
        /// Windows 11. No-op on older Windows. For dialogs that don't call FitToScreen.</summary>
        public static void RoundCorners(Window w)
        {
            void Apply()
            {
                RoundCornersHwnd(new WindowInteropHelper(w).Handle);
            }
            if (w.IsLoaded) Apply();
            else w.SourceInitialized += (_, __) => Apply();
        }

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private static void RoundCornersHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { /* pre-Win11 — ignore */ }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // ── Constrain maximize to the monitor work area ─────────────────────────

        private const int WM_GETMINMAXINFO = 0x0024;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(monitor, ref info))
                    {
                        var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                        RECT work = info.rcWork, mon = info.rcMonitor;
                        mmi.ptMaxPosition.x = work.left - mon.left;
                        mmi.ptMaxPosition.y = work.top - mon.top;
                        mmi.ptMaxSize.x = work.right - work.left;
                        mmi.ptMaxSize.y = work.bottom - work.top;
                        Marshal.StructureToPtr(mmi, lParam, true);
                    }
                }
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }
    }
}
