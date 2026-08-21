using System.Xml.Linq;

namespace RCDragManagerProd.Tests
{
    /// <summary>
    /// Guards the window sizing standard (#420) against the race-day laptop as the
    /// minimum supported display (#421): 1920x1080 at 150% scaling = 1280x720 DIU,
    /// leaving 1280x688 usable once the taskbar is subtracted.
    ///
    /// These read the WPF XAML as text rather than loading the assembly — the test
    /// project deliberately does not reference RCDragManagerProd.WPF.
    /// </summary>
    [TestClass]
    public sealed class WindowSizingStandardTests
    {
        private const double MinDisplayWidth = 1280;
        private const double MinDisplayHeight = 688;

        // The shared workspace footprint. Every window the operator navigates
        // between opens at exactly this size so the footprint never jumps.
        private const double WorkspaceWidth = 1180;
        private const double WorkspaceHeight = 660;

        private static readonly string[] WorkspaceWindows =
        {
            "LandingWindow", "SetupWindow", "LoadSessionWindow",
            "DriverManagerWindow", "RaceConsoleWindow", "MultiClassRaceWindow",
        };

        private sealed class WindowSize
        {
            public WindowSize(string name, double? width, double? height,
                              double? minWidth, double? minHeight)
            {
                Name = name;
                Width = width;
                Height = height;
                MinWidth = minWidth;
                MinHeight = minHeight;
            }

            public string Name { get; }
            public double? Width { get; }
            public double? Height { get; }
            public double? MinWidth { get; }
            public double? MinHeight { get; }
        }

        [TestMethod]
        public void EveryWindow_FitsTheMinimumSupportedDisplay()
        {
            var failures = new List<string>();

            foreach (var w in LoadWindowSizes())
            {
                if (w.Width > MinDisplayWidth)
                    failures.Add($"{w.Name}: Width {w.Width} > {MinDisplayWidth}");
                if (w.Height > MinDisplayHeight)
                    failures.Add($"{w.Name}: Height {w.Height} > {MinDisplayHeight}");

                // A MinWidth/MinHeight above the display is worse than a large
                // default — the window cannot be shrunk to fit at all.
                if (w.MinWidth > MinDisplayWidth)
                    failures.Add($"{w.Name}: MinWidth {w.MinWidth} > {MinDisplayWidth}");
                if (w.MinHeight > MinDisplayHeight)
                    failures.Add($"{w.Name}: MinHeight {w.MinHeight} > {MinDisplayHeight}");
            }

            Assert.AreEqual(0, failures.Count,
                "Windows exceed the race-day laptop canvas (1280x688):\n" +
                string.Join("\n", failures));
        }

        [TestMethod]
        public void WorkspaceWindows_ShareOneFootprint()
        {
            var byName = LoadWindowSizes().ToDictionary(w => w.Name);
            var failures = new List<string>();

            foreach (var name in WorkspaceWindows)
            {
                if (!byName.TryGetValue(name, out var w))
                {
                    failures.Add($"{name}: not found");
                    continue;
                }

                if (w.Width != WorkspaceWidth || w.Height != WorkspaceHeight)
                    failures.Add($"{name}: {w.Width}x{w.Height}, expected " +
                                 $"{WorkspaceWidth}x{WorkspaceHeight}");
            }

            Assert.AreEqual(0, failures.Count,
                "Workspace windows must all open at the same size so navigating " +
                "between them does not change the window footprint:\n" +
                string.Join("\n", failures));
        }

        // ── Loading ───────────────────────────────────────────────────────────

        private static List<WindowSize> LoadWindowSizes()
        {
            var wpf = FindWpfProjectDirectory();
            var files = Directory.GetFiles(Path.Combine(wpf, "Windows"), "*.xaml")
                .Concat(Directory.GetFiles(Path.Combine(wpf, "Dialogs"), "*.xaml"))
                .OrderBy(f => f);

            var sizes = new List<WindowSize>();
            foreach (var file in files)
            {
                var root = XDocument.Load(file).Root;
                if (root == null) continue;

                // Only real windows declare sizes; UserControls and the like don't.
                if (root.Name.LocalName != "Window") continue;

                sizes.Add(new WindowSize(
                    Path.GetFileNameWithoutExtension(file),
                    ParseSize(root.Attribute("Width")?.Value),
                    ParseSize(root.Attribute("Height")?.Value),
                    ParseSize(root.Attribute("MinWidth")?.Value),
                    ParseSize(root.Attribute("MinHeight")?.Value)));
            }

            Assert.IsTrue(sizes.Count > 0, $"No Window XAML found under {wpf}");
            return sizes;
        }

        // "Auto"/"*" and anything non-numeric means the window is content-sized,
        // which the display-fit rules can't judge from markup alone.
        private static double? ParseSize(string? raw) =>
            double.TryParse(raw, out var v) ? v : (double?)null;

        private static string FindWpfProjectDirectory()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "RCDragManagerProd.WPF");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate src/RCDragManagerProd.WPF above " +
                AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
