using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// The end-of-event board: every class with its champion and runner-up. Replaces
    /// the ASCII text block the multi-class window used to dump into a plain text
    /// dialog when the last class finished.
    /// </summary>
    public partial class EventCompletionWindow : Window
    {
        private readonly EventCompletionPresentation _view;

        public EventCompletionWindow(MultiClassEvent multiEvent)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            _view = EventCompletionPresentationBuilder.Build(multiEvent);
            DataContext = _view;
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            // Posting results to the club chat was the one thing the old text dialog
            // was good for, so the plain-text rendering survives as a button.
            try
            {
                Clipboard.SetText(_view.CopyText ?? "");
                LblCopied.Visibility = Visibility.Visible;
            }
            catch { /* clipboard can be locked by another app; not worth interrupting */ }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
