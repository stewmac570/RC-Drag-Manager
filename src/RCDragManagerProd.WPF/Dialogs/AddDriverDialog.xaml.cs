using System;
using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// Bulk-entry add-driver dialog (#417). Race directors add 10–20 drivers in a
    /// burst at setup, so the dialog stays open: Enter runs "Add &amp; next", which
    /// commits the driver, clears the fields and puts the caret back in the name
    /// box. Validation errors show inline rather than closing the dialog.
    /// </summary>
    public partial class AddDriverDialog : Window
    {
        /// <summary>
        /// Commits one entry. Returns an operator-facing error message, or null when
        /// the driver was accepted. Supplied by the console view, which owns the
        /// roster; the dialog holds no roster logic of its own.
        /// </summary>
        private readonly Func<string, string, string> _tryAdd;

        public int AddedCount { get; private set; }

        public AddDriverDialog(Func<string, string, string> tryAdd)
        {
            if (tryAdd == null) throw new ArgumentNullException(nameof(tryAdd));
            _tryAdd = tryAdd;
            InitializeComponent();
            Loaded += (_, __) => TxtName.Focus();
        }

        private void BtnAddNext_Click(object sender, RoutedEventArgs e)
        {
            var error = _tryAdd(TxtName.Text, TxtQual.Text);
            if (error != null)
            {
                ShowError(error);
                TxtName.Focus();
                TxtName.SelectAll();
                return;
            }

            AddedCount++;
            ErrorText.Visibility = Visibility.Collapsed;
            AddedText.Text = AddedCount == 1
                ? "1 driver added — keep going, or Close when you're done."
                : $"{AddedCount} drivers added — keep going, or Close when you're done.";
            AddedText.Visibility = Visibility.Visible;

            TxtName.Clear();
            TxtQual.Clear();
            TxtName.Focus();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        // Clear a stale error as soon as the operator starts fixing it.
        private void Field_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ErrorText != null) ErrorText.Visibility = Visibility.Collapsed;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
