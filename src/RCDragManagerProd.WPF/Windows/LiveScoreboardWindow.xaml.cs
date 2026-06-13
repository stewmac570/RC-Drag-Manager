using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using QRCoder;
using RCDragManagerProd.Logging;
using RCDragManagerProd.WPF.Dialogs;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class LiveScoreboardWindow : Window
    {
        private const string LiveScoreboardUrl = "https://stewmacrc.com";

        public LiveScoreboardWindow()
        {
            InitializeComponent();
            UrlText.Text = LiveScoreboardUrl;
            QrImage.Source = BuildQrImage(LiveScoreboardUrl);
        }

        // PngByteQRCode avoids a System.Drawing dependency — straight PNG bytes into a WPF image.
        private static BitmapImage BuildQrImage(string url)
        {
            using (var generator = new QRCodeGenerator())
            {
                var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
                var png = new PngByteQRCode(data);
                byte[] bytes = png.GetGraphic(20);

                var img = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = ms;
                    img.EndInit();
                }
                img.Freeze();
                return img;
            }
        }

        private void BtnOpenLiveView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = LiveScoreboardUrl, UseShellExecute = true });
                Logger.Log("[LIVE][OPEN] " + LiveScoreboardUrl);
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][FAIL] " + ex.Message);
                MessageDialog.Error(this, "Could not open live view.\n\n" + ex.Message, "Live view");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
