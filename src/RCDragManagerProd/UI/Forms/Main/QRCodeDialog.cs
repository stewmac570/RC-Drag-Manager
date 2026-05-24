using System;
using System.Drawing;
using System.Windows.Forms;
using QRCoder;

namespace RCDragManagerProd.UI.Forms
{
    internal class QRCodeDialog : Form
    {
        private const string LiveScoreboardUrl = "https://stewmacrc.com";

        private PictureBox _pictureBoxQr;
        private Label _lblInstruction;
        private Button _btnClose;

        internal QRCodeDialog()
        {
            InitializeComponent();
            GenerateQRCode();
        }

        private void InitializeComponent()
        {
            this.Text = "Live Scoreboard QR Code";
            this.ClientSize = new Size(500, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            _lblInstruction = new Label();
            _lblInstruction.Text = "Scan to open the live scoreboard\n" + LiveScoreboardUrl;
            _lblInstruction.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            _lblInstruction.ForeColor = Color.FromArgb(30, 30, 30);
            _lblInstruction.TextAlign = ContentAlignment.MiddleCenter;
            _lblInstruction.Location = new Point(10, 12);
            _lblInstruction.Size = new Size(480, 58);

            _pictureBoxQr = new PictureBox();
            _pictureBoxQr.Location = new Point(50, 80);
            _pictureBoxQr.Size = new Size(400, 400);
            _pictureBoxQr.SizeMode = PictureBoxSizeMode.StretchImage;
            _pictureBoxQr.BackColor = Color.White;
            _pictureBoxQr.BorderStyle = BorderStyle.FixedSingle;

            _btnClose = new Button();
            _btnClose.Text = "Close";
            _btnClose.Font = new Font("Segoe UI", 11F);
            _btnClose.Location = new Point(190, 510);
            _btnClose.Size = new Size(120, 40);
            _btnClose.Click += (s, e) => Close();

            this.Controls.Add(_lblInstruction);
            this.Controls.Add(_pictureBoxQr);
            this.Controls.Add(_btnClose);
        }

        private void GenerateQRCode()
        {
            using (var generator = new QRCodeGenerator())
            {
                QRCodeData data = generator.CreateQrCode(LiveScoreboardUrl, QRCodeGenerator.ECCLevel.M);
                using (var qrCode = new QRCode(data))
                {
                    _pictureBoxQr.Image = qrCode.GetGraphic(20);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pictureBoxQr.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
