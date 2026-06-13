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
    /// </summary>
    public static class WindowSizing
    {
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
