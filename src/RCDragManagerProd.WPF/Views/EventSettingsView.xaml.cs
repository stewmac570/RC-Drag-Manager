using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RCDragManagerProd.Config;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Views
{
    /// <summary>
    /// First tab of an event: per-class administration (reset, buybacks) and
    /// appearance (#415). Holds no race logic — the host window owns the
    /// controllers and supplies the two callbacks below, and the rules for what
    /// may change live in <c>EventSettingsService</c>.
    /// </summary>
    public partial class EventSettingsView : UserControl
    {
        /// <summary>Resets the class at this index. Returns an error, or null on success.</summary>
        public Func<int, string> ResetClass { get; set; }

        /// <summary>Turns buybacks on/off for the class at this index. Returns an error, or null.</summary>
        public Func<int, bool, string> SetBuybacks { get; set; }

        /// <summary>Rebuilds the class rows; the host calls this whenever class state moves.</summary>
        public Action RequestRefresh { get; set; }

        private bool _applyingTheme;

        public EventSettingsView()
        {
            InitializeComponent();

            _applyingTheme = true;
            bool light = ThemeManager.FromSetting() == ThemeManager.AppTheme.Light;
            RbLight.IsChecked = light;
            RbDark.IsChecked = !light;
            _applyingTheme = false;
        }

        public void SetHeader(string eventName, string meta)
        {
            LblEventName.Text = string.IsNullOrWhiteSpace(eventName) ? "Untitled event" : eventName;
            LblEventMeta.Text = meta;
        }

        public void SetClasses(IReadOnlyList<EventSettingsRow> rows) => IcClasses.ItemsSource = rows;

        private Window Host => Window.GetWindow(this);

        // ── Class actions ─────────────────────────────────────────────────────

        private void ResetClass_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetRow(sender, out var row)) return;

            var dlg = new Dialogs.ResetClassDialog(row.ClassName, row.Status) { Owner = Host };
            if (dlg.ShowDialog() != true) return;

            var error = ResetClass?.Invoke(row.Index);
            if (error != null)
                Dialogs.MessageDialog.Warn(Host, error, "Reset class");

            RequestRefresh?.Invoke();
        }

        private void Buybacks_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetRow(sender, out var row)) return;

            // Bound OneWay, so the checkbox's new visual state is the requested value
            // and the row is only rewritten once the change is accepted.
            var wanted = (sender as CheckBox)?.IsChecked == true;

            var error = SetBuybacks?.Invoke(row.Index, wanted);
            if (error != null)
                Dialogs.MessageDialog.Warn(Host, error, "Buybacks");

            RequestRefresh?.Invoke();
        }

        private static bool TryGetRow(object sender, out EventSettingsRow row)
        {
            row = (sender as FrameworkElement)?.DataContext as EventSettingsRow;
            return row != null;
        }

        // ── Appearance ────────────────────────────────────────────────────────

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            if (_applyingTheme) return;

            var theme = RbLight.IsChecked == true
                ? ThemeManager.AppTheme.Light
                : ThemeManager.AppTheme.Dark;

            // Applied live rather than on restart (which is what the Settings window
            // does): restarting mid-event would drop unsaved race progress.
            ThemeManager.Apply(theme);
            AppSettings.Theme = theme.ToString();
        }
    }
}
