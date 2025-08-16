using System;
using System.Drawing;
using System.Windows.Forms;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public sealed class ScrollableTextDialog : Form
    {
        private readonly RichTextBox _rtb;
        private readonly Button _btnCopy;
        private readonly Button _btnClose;

        public static void Show(string title, string content)
        {
            Logger.Log($"[UI][SCROLL] Open: {title} (len={content?.Length ?? 0})");
            using (var dlg = new ScrollableTextDialog(title, content))
            {
                dlg.ShowDialog();
            }
            Logger.Log("[UI][SCROLL] Closed");
        }

        private ScrollableTextDialog(string title, string content)
        {
            Text = title ?? "Details";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            ClientSize = new Size(820, 640);

            _rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                DetectUrls = false,
                WordWrap = false,                   // ← horizontal scroll enabled
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 9.0f),
                Text = content ?? string.Empty
            };

            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48
            };

            _btnCopy = new Button
            {
                Text = "Copy",
                Width = 90,
                Height = 28,
                Left = 8,
                Top = 10
            };
            _btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(_rtb.Text ?? string.Empty);
                    Logger.Log("[UI][SCROLL] Copied to clipboard");
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UI][SCROLL][ERROR] Copy failed: {ex.Message}");
                }
            };

            _btnClose = new Button
            {
                Text = "Close",
                Width = 90,
                Height = 28,
                Left = ClientSize.Width - 98,
                Top = 10,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnClose.Click += (s, e) => Close();

            pnl.Controls.Add(_btnCopy);
            pnl.Controls.Add(_btnClose);

            Controls.Add(_rtb);
            Controls.Add(pnl);

            AcceptButton = _btnClose;
        }
    }
}
