using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using RCDragManagerProd.Config;

namespace RCDragManagerProd.WPF
{
    /// <summary>
    /// Runtime dark/light theming. Theme.xaml defines Color resources (C.*) and brushes
    /// (Brush.*) whose Color binds to them via DynamicResource. Swapping a C.* colour
    /// updates every brush — and therefore every StaticResource consumer — live, with no
    /// per-window XAML changes. (The DynamicResource binding also keeps the brushes
    /// unfreezable, which is why directly mutating brush colours failed.)
    /// </summary>
    public static class ThemeManager
    {
        public enum AppTheme { Dark, Light }

        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        // colour-resource key → (dark hex, light hex). Light values are the approved
        // design system (flame orange on warm off-white).
        private static readonly Dictionary<string, (string Dark, string Light)> Palette =
            new Dictionary<string, (string, string)>
            {
                ["C.Background"]      = ("#0D0D0D", "#F5F3F0"),
                ["C.Surface"]        = ("#181818", "#FFFFFF"),
                ["C.Raised"]         = ("#252525", "#EDEBE7"),
                ["C.Chrome"]         = ("#161616", "#ECEAE6"),

                ["C.Primary"]        = ("#E85010", "#D44808"),
                ["C.PrimaryHover"]   = ("#F06522", "#E85A18"),
                ["C.PrimaryPressed"] = ("#D04008", "#B83C06"),
                ["C.Accent"]         = ("#FF8A00", "#E87808"),

                ["C.Success"]        = ("#2A8E60", "#1D7A50"),
                ["C.Danger"]         = ("#C84040", "#B83030"),

                ["C.TextPrimary"]    = ("#F0F0F0", "#141414"),
                ["C.TextSecondary"]  = ("#909090", "#5A5A5A"),
                ["C.TextMuted"]      = ("#505050", "#8A8A8A"),
                ["C.TextHint"]       = ("#444444", "#A8A8A8"),
                ["C.TextGhost"]      = ("#333333", "#C5C5C5"),

                ["C.Border"]         = ("#17FFFFFF", "#14000000"),
                ["C.BorderSubtle"]   = ("#0FFFFFFF", "#0A000000"),
                ["C.BorderEmphasis"] = ("#252525", "#D0CEC9"),
                ["C.WindowEdge"]     = ("#3C3C3C", "#C7C4BE"),
            };

        public static AppTheme FromSetting() =>
            string.Equals(AppSettings.Theme, "Light", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Light : AppTheme.Dark;

        // The currently-installed colours dictionary, so it can be swapped wholesale.
        private static ResourceDictionary _colors;

        public static void Apply(AppTheme theme)
        {
            Current = theme;
            var app = Application.Current;
            if (app == null) return;

            var next = new ResourceDictionary();
            foreach (var kvp in Palette)
            {
                var hex = theme == AppTheme.Light ? kvp.Value.Light : kvp.Value.Dark;
                next[kvp.Key] = (Color)ColorConverter.ConvertFromString(hex);
            }

            // Swapping the whole merged dictionary reliably re-resolves every
            // DynamicResource C.* — so all open windows re-theme live.
            if (_colors != null) app.Resources.MergedDictionaries.Remove(_colors);
            app.Resources.MergedDictionaries.Add(next);
            _colors = next;
        }
    }
}
