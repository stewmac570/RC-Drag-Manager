using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RCDragManagerProd.Tests
{
    /// <summary>
    /// Race-day feedback (Oct 2026): forms were all different sizes, some could not be
    /// resized, and one ran off the screen. Every dialog is now one of three sizes, can
    /// be resized, and keeps itself on screen. Reads the XAML and code-behind as text,
    /// like <see cref="WindowSizingStandardTests"/>; the test project does not reference
    /// the WPF assembly.
    /// </summary>
    [TestClass]
    public sealed class DialogSizingStandardTests
    {
        private const double Small = 440;
        private const double MediumWidth = 600, MediumHeight = 560;
        private const double LargeWidth = 1040, LargeHeight = 660;

        [TestMethod]
        public void EveryDialog_IsSmallMediumOrLarge()
        {
            var failures = new List<string>();
            foreach (var d in LoadDialogs())
            {
                bool small = d.Width == Small && d.SizeToContent == "Height";
                bool medium = d.Width == MediumWidth && d.Height == MediumHeight;
                bool large = d.Width == LargeWidth && d.Height == LargeHeight;
                if (!(small || medium || large))
                    failures.Add($"{d.Name}: {d.Width}x{d.Height?.ToString() ?? d.SizeToContent}");
            }

            Assert.AreEqual(0, failures.Count,
                $"Dialogs must be small ({Small} wide, height to content), medium ({MediumWidth}x{MediumHeight}) " +
                $"or large ({LargeWidth}x{LargeHeight}):\n" + string.Join("\n", failures));
        }

        [TestMethod]
        public void EveryDialog_CanBeResized()
        {
            var failures = LoadDialogs()
                .Where(d => d.ResizeMode != "CanResize" && d.ResizeMode != "CanResizeWithGrip")
                .Select(d => $"{d.Name}: ResizeMode={d.ResizeMode ?? "(default)"}")
                .ToList();
            failures.AddRange(LoadDialogs()
                .Where(d => d.ResizeBorder == "0")
                .Select(d => $"{d.Name}: WindowChrome ResizeBorderThickness=0 (cannot be dragged)"));

            Assert.AreEqual(0, failures.Count, "Every dialog must be resizable:\n" + string.Join("\n", failures));
        }

        [TestMethod]
        public void EveryDialog_KeepsItselfOnScreen()
        {
            var failures = LoadDialogs()
                .Where(d => !Regex.IsMatch(d.CodeBehind, @"WindowSizing\.(FitToScreen|FitDialogToScreen)\(this\)"))
                .Select(d => d.Name)
                .ToList();

            Assert.AreEqual(0, failures.Count,
                "Every dialog must call WindowSizing.FitToScreen or FitDialogToScreen:\n" + string.Join("\n", failures));
        }

        [TestMethod]
        public void SmallDialogs_ScrollWhenTheScreenIsShort()
        {
            var failures = LoadDialogs()
                .Where(d => d.SizeToContent == "Height" && !d.Xaml.Contains("<ScrollViewer"))
                .Select(d => d.Name)
                .ToList();

            Assert.AreEqual(0, failures.Count,
                "A content-sized dialog must wrap its content in a ScrollViewer so it never runs off a short screen:\n" +
                string.Join("\n", failures));
        }

        // ── Loading ───────────────────────────────────────────────────────────

        private sealed class Dialog
        {
            public string Name = "";
            public double? Width, Height;
            public string? SizeToContent, ResizeMode, ResizeBorder;
            public string Xaml = "", CodeBehind = "";
        }

        private static List<Dialog> LoadDialogs()
        {
            var dir = Path.Combine(FindWpfProjectDirectory(), "Dialogs");
            var list = new List<Dialog>();
            foreach (var file in Directory.GetFiles(dir, "*.xaml").OrderBy(f => f))
            {
                var root = XDocument.Load(file).Root;
                if (root == null || root.Name.LocalName != "Window") continue;

                var xaml = File.ReadAllText(file);
                var codeFile = file + ".cs";
                list.Add(new Dialog
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Width = ParseSize(root.Attribute("Width")?.Value),
                    Height = ParseSize(root.Attribute("Height")?.Value),
                    SizeToContent = root.Attribute("SizeToContent")?.Value,
                    ResizeMode = root.Attribute("ResizeMode")?.Value,
                    ResizeBorder = Regex.Match(xaml, "ResizeBorderThickness=\"([^\"]*)\"") is var m && m.Success
                        ? m.Groups[1].Value : null,
                    Xaml = xaml,
                    CodeBehind = File.Exists(codeFile) ? File.ReadAllText(codeFile) : ""
                });
            }

            Assert.IsTrue(list.Count > 0, $"No dialog XAML found under {dir}");
            return list;
        }

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
            throw new DirectoryNotFoundException("Could not locate src/RCDragManagerProd.WPF");
        }
    }
}
