using System;
using System.Diagnostics;
using System.Windows.Forms;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public sealed class SettingsForm : Form
    {
        private const string ProductionLiveViewUrl = "https://stewmacrc.com";

        private readonly CheckBox _chkLogging;
        private readonly TextBox _txtPath;
        private readonly Button _btnStartLocalLiveServer;
        private readonly Button _btnOpenLiveView;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public SettingsForm()
        {
            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 460;
            Height = 250;

            _chkLogging = new CheckBox
            {
                Left = 12,
                Top = 12,
                Width = 400,
                Text = "Enable logging",
                Checked = AppSettings.EnableLogging
            };

            var lblPath = new Label
            {
                Left = 12,
                Top = 45,
                Width = 80,
                Text = "Log file:"
            };

            _txtPath = new TextBox
            {
                Left = 90,
                Top = 42,
                Width = 340,
                ReadOnly = true,
                Text = AppSettings.LogFilePath
            };

            _btnStartLocalLiveServer = new Button
            {
                Name = "btnStartLocalLiveServer",
                Text = "Test Local Server (DEV ONLY)",
                Left = 12,
                Top = 78,
                Width = 200,
                Enabled = false
            };

            _btnOpenLiveView = new Button
            {
                Name = "btnOpenLiveView",
                Text = "Open Live View",
                Left = 230,
                Top = 78,
                Width = 200
            };

            _btnOk = new Button { Text = "Save", Left = 270, Top = 150, Width = 75, DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Cancel", Left = 355, Top = 150, Width = 75, DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[]
            {
                _chkLogging,
                lblPath,
                _txtPath,
                _btnStartLocalLiveServer,
                _btnOpenLiveView,
                _btnOk,
                _btnCancel
            });

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            _btnStartLocalLiveServer.Click += btnStartLocalLiveServer_Click;
            _btnOpenLiveView.Click += btnOpenLiveView_Click;

            _btnOk.Click += (_, __) =>
            {
                AppSettings.EnableLogging = _chkLogging.Checked;
                if (AppSettings.EnableLogging) Logger.Log("[SETTINGS] Logging enabled.");
                Close();
            };
        }

        private void btnStartLocalLiveServer_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "Local server start is disabled in production builds.",
                "DEV ONLY",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnOpenLiveView_Click(object sender, EventArgs e)
        {
            _btnOpenLiveView.Enabled = false;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProductionLiveViewUrl,
                    UseShellExecute = true
                });

                Logger.Log("[LIVE][OPEN] " + ProductionLiveViewUrl);
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][FAIL] Open live view failed. " + ex.Message);
                MessageBox.Show(this,
                    "Could not open live view.\n\n" + ex.Message,
                    "Live View",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _btnOpenLiveView.Enabled = true;
            }
        }
    }
}
