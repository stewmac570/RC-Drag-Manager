using System;
using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// Typed confirmation for resetting a class (#415). Reset used to be a single
    /// click on the race console, which is how a whole class bracket got wiped at a
    /// meet (#413). Here the operator has to type the class name, so it can't happen
    /// by reflex.
    /// </summary>
    public partial class ResetClassDialog : Window
    {
        private readonly string _className;

        public ResetClassDialog(string className, string statusText)
        {
            _className = (className ?? "").Trim();
            InitializeComponent();

            BodyText.Text =
                $"This clears the bracket, winners and round progress for “{_className}” " +
                $"(currently {statusText.ToLowerInvariant()}). Drivers stay in the class. " +
                "This cannot be undone.";
            PromptText.Text = $"Type the class name to confirm:  {_className}";

            Loaded += (_, __) => TxtConfirm.Focus();
        }

        private void TxtConfirm_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (BtnReset == null) return;
            BtnReset.IsEnabled = string.Equals(TxtConfirm.Text.Trim(), _className,
                                               StringComparison.OrdinalIgnoreCase);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnReset.IsEnabled) return;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
