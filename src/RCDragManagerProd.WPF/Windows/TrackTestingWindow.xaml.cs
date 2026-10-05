using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Logging;
using RCDragManagerProd.WPF.Dialogs;

namespace RCDragManagerProd.WPF.Windows
{
    /// <summary>
    /// Open while the track is in testing. Opening starts track testing (times on the
    /// stream, no names, no event); closing the window, however it closes, stops it.
    /// </summary>
    public partial class TrackTestingWindow : Window
    {
        private readonly TrackTestingService _testing = new TrackTestingService();

        public TrackTestingWindow()
        {
            InitializeComponent();
            WindowSizing.FitDialogToScreen(this);
            Loaded += (_, __) => StartTesting();
        }

        private void StartTesting()
        {
            try
            {
                _testing.Start();
            }
            catch (Exception ex)
            {
                Logger.Log("[TESTING][FAIL] " + ex);
                MessageDialog.Error(this, "Could not start track testing.\n\n" + ex.Message, "Track testing");
                Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try { _testing.Stop(); }
            catch (Exception ex) { Logger.Log("[TESTING][FAIL] stop: " + ex.Message); }
            base.OnClosing(e);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
