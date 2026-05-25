using System.Windows.Forms;
using System.Drawing;
using RCDragManagerProd.Properties;

namespace RCDragManagerProd.UI.Forms
{
    partial class LandingForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblEventTitle;
        private Button btnCreateRaceSession;
        private Button btnLoadEvent;
        private Button btnDriverLists;
        private Button btnSettings;
        private Button btnExit;
        private PictureBox logoBox;
        private Label lblVersion;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblEventTitle = new Label();
            this.btnCreateRaceSession = new Button();
            this.btnLoadEvent = new Button();
            this.btnDriverLists = new Button();
            this.btnSettings = new Button();
            this.btnExit = new Button();
            this.lblVersion = new Label();
            this.logoBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.logoBox)).BeginInit();
            this.SuspendLayout();

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "RC Drag Manager";

            // lblEventTitle
            this.lblEventTitle.Location = new System.Drawing.Point(20, 10);
            this.lblEventTitle.Size = new System.Drawing.Size(860, 30);
            this.lblEventTitle.Text = "RC Drag Manager";
            this.lblEventTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblEventTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(this.lblEventTitle);

            // btnCreateRaceSession
            this.btnCreateRaceSession.Location = new System.Drawing.Point(40, 100);
            this.btnCreateRaceSession.Size = new System.Drawing.Size(200, 50);
            this.btnCreateRaceSession.Text = "Create Race Session";
            this.btnCreateRaceSession.Click += new System.EventHandler(this.btnCreateRaceSession_Click);
            this.Controls.Add(this.btnCreateRaceSession);

            // btnLoadEvent
            this.btnLoadEvent.Location = new System.Drawing.Point(40, 170);
            this.btnLoadEvent.Size = new System.Drawing.Size(200, 50);
            this.btnLoadEvent.Text = "Load Saved Event";
            this.btnLoadEvent.Click += new System.EventHandler(this.btnLoadEvent_Click);
            this.Controls.Add(this.btnLoadEvent);

            // btnDriverLists
            this.btnDriverLists.Location = new System.Drawing.Point(40, 240);
            this.btnDriverLists.Size = new System.Drawing.Size(200, 50);
            this.btnDriverLists.Text = "Driver Lists";
            this.btnDriverLists.Click += new System.EventHandler(this.btnDriverLists_Click);
            this.Controls.Add(this.btnDriverLists);

            // btnSettings
            this.btnSettings.Location = new System.Drawing.Point(40, 310);
            this.btnSettings.Size = new System.Drawing.Size(200, 50);
            this.btnSettings.Text = "Settings";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            this.Controls.Add(this.btnSettings);

            // btnExit
            this.btnExit.Location = new System.Drawing.Point(40, 380);
            this.btnExit.Size = new System.Drawing.Size(200, 50);
            this.btnExit.Text = "Exit";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            this.Controls.Add(this.btnExit);

            // lblVersion
            this.lblVersion.Location = new System.Drawing.Point(20, 570);
            this.lblVersion.Size = new System.Drawing.Size(200, 20);
            this.lblVersion.Text = "v1.4.0";
            this.Controls.Add(this.lblVersion);

            // logoBox
            this.logoBox.Location = new System.Drawing.Point(260, 80);
            this.logoBox.Size = new System.Drawing.Size(590, 396);
            this.logoBox.SizeMode = PictureBoxSizeMode.StretchImage;
            this.logoBox.Image = Properties.Resources.Reto_logo_trans_full;
            this.Controls.Add(this.logoBox);


            ((System.ComponentModel.ISupportInitialize)(this.logoBox)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
