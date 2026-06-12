using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using RCDragManagerProd.Config;

namespace RCDragManagerProd.WPF
{
    /// <summary>
    /// Runtime dark/light theming. The named theme brushes in Theme.xaml are shared
    /// instances referenced (via StaticResource) throughout the app, so mutating each
    /// brush's Color re-themes every open and future window live — no per-window XAML
    /// changes needed.
    /// </summary>
    public static class ThemeManager
    {
        public enum AppTheme { Dark, Light }

        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        // key → (dark hex, light hex). Light values come from the approved design system
        // (flame orange on warm off-white).
        private static readonly Dictionary<string, (string Dark, string Light)> Palette =
            new Dictionary<string, (string, string)>
            {
                ["Brush.Background"]      = ("#0D0D0D", "#F5F3F0"),
                ["Brush.Surface"]        = ("#181818", "#FFFFFF"),
                ["Brush.Raised"]         = ("#252525", "#EDEBE7"),
                ["Brush.Chrome"]         = ("#161616", "#ECEAE6"),

                ["Brush.Primary"]        = ("#E85010", "#D44808"),
                ["Brush.PrimaryHover"]   = ("#F06522", "#E85A18"),
                ["Brush.PrimaryPressed"] = ("#D04008", "#B83C06"),
                ["Brush.Accent"]         = ("#FF8A00", "#E87808"),

                ["Brush.Success"]        = ("#2A8E60", "#1D7A50"),
                ["Brush.Danger"]         = ("#C84040", "#B83030"),

                ["Brush.TextPrimary"]    = ("#F0F0F0", "#141414"),
                ["Brush.TextSecondary"]  = ("#909090", "#5A5A5A"),
                ["Brush.TextMuted"]      = ("#505050", "#8A8A8A"),
                ["Brush.TextHint"]       = ("#444444", "#A8A8A8"),
                ["Brush.TextGhost"]      = ("#333333", "#C5C5C5"),

                ["Brush.Border"]         = ("#17FFFFFF", "#14000000"),
                ["Brush.BorderSubtle"]   = ("#0FFFFFFF", "#0A000000"),
                ["Brush.BorderEmphasis"] = ("#252525", "#D0CEC9"),
            };

        public static AppTheme FromSetting() =>
            string.Equals(AppSettings.Theme, "Light", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Light : AppTheme.Dark;

        /// <summary>
        /// Builds the named theme brushes in code (so they are NOT frozen and can be
        /// re-coloured live). Must be merged into App resources before Styles.xaml so
        /// the styles' StaticResource references resolve against these brushes.
        /// </summary>
        public static ResourceDictionary BuildBrushDictionary(AppTheme theme)
        {
            var dict = new ResourceDictionary();
            foreach (var kvp in Palette)
            {
                var hex = theme == AppTheme.Light ? kvp.Value.Light : kvp.Value.Dark;
                dict[kvp.Key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            Current = theme;
            return dict;
        }

        public static void Apply(AppTheme theme)
        {
            Current = theme;
            var res = Application.Current?.Resources;
            if (res == null) return;

            foreach (var kvp in Palette)
            {
                if (res[kvp.Key] is SolidColorBrush brush && !brush.IsFrozen)
                {
                    var hex = theme == AppTheme.Light ? kvp.Value.Light : kvp.Value.Dark;
                    brush.Color = (Color)ColorConverter.ConvertFromString(hex);
                }
            }
        }
    }
}
