using System.Windows.Forms;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public sealed class SettingsForm : Form
    {
        private readonly CheckBox _chkLogging;
        private readonly TextBox _txtPath;
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
            Height = 180;

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

            _btnOk = new Button { Text = "Save", Left = 270, Top = 90, Width = 75, DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Cancel", Left = 355, Top = 90, Width = 75, DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { _chkLogging, lblPath, _txtPath, _btnOk, _btnCancel });

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            _btnOk.Click += (_, __) =>
            {
                AppSettings.EnableLogging = _chkLogging.Checked;
                if (AppSettings.EnableLogging) Logger.Log("[SETTINGS] Logging enabled.");
                Close();
            };
        }
    }
}
