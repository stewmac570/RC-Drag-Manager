using System.Windows.Forms;
using System.Drawing;
using RCDragManagerProd.Properties;

namespace RCDragManagerProd.UI.Forms
{
    partial class LandingForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblEventTitle;
        private Button btnNewEvent;
        private Button btnCreateSession;
        private Button btnLoadEvent;
        private Button btnDriverLists;
        private Button btnSettings;
        private Button btnNewMultiClassEvent;
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
            this.btnNewEvent = new Button();
            this.btnCreateSession = new Button();
            this.btnLoadEvent = new Button();
            this.btnDriverLists = new Button();
            this.btnSettings = new Button();
            this.btnNewMultiClassEvent = new Button();
            this.btnExit = new Button();
            this.lblVersion = new Label();
            this.logoBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.logoBox)).BeginInit();
            this.SuspendLayout();

            // Form
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

            // btnNewEvent (Quick Session)
            this.btnNewEvent.Location = new System.Drawing.Point(40, 80);
            this.btnNewEvent.Size = new System.Drawing.Size(200, 50);
            this.btnNewEvent.Text = "Quick Session";
            this.btnNewEvent.Click += new System.EventHandler(this.btnNewEvent_Click);
            this.Controls.Add(this.btnNewEvent);

            // btnCreateSession (New Build)
            this.btnCreateSession.Location = new System.Drawing.Point(40, 150);
            this.btnCreateSession.Size = new System.Drawing.Size(200, 50);
            this.btnCreateSession.Text = "Create Race Session";
            this.btnCreateSession.Click += new System.EventHandler(this.btnCreateSession_Click);
            this.Controls.Add(this.btnCreateSession);

            // btnLoadEvent (Future)
            this.btnLoadEvent.Location = new System.Drawing.Point(40, 220);
            this.btnLoadEvent.Size = new System.Drawing.Size(200, 50);
            this.btnLoadEvent.Text = "Load Saved Event";
            this.btnLoadEvent.Click += new System.EventHandler(this.btnLoadEvent_Click);
            this.Controls.Add(this.btnLoadEvent);

            // btnDriverLists
            this.btnDriverLists.Location = new System.Drawing.Point(40, 290);
            this.btnDriverLists.Size = new System.Drawing.Size(200, 50);
            this.btnDriverLists.Text = "Driver Lists";
            this.btnDriverLists.Click += new System.EventHandler(this.btnDriverLists_Click);
            this.Controls.Add(this.btnDriverLists);

            // btnSettings
            this.btnSettings.Location = new System.Drawing.Point(40, 360);
            this.btnSettings.Size = new System.Drawing.Size(200, 50);
            this.btnSettings.Text = "Settings";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            this.Controls.Add(this.btnSettings);

            // btnNewMultiClassEvent
            this.btnNewMultiClassEvent.Location = new System.Drawing.Point(40, 430);
            this.btnNewMultiClassEvent.Size = new System.Drawing.Size(200, 50);
            this.btnNewMultiClassEvent.Text = "New Multi-Class Event";
            this.btnNewMultiClassEvent.Click += new System.EventHandler(this.btnNewMultiClassEvent_Click);
            this.Controls.Add(this.btnNewMultiClassEvent);

            // btnExit
            this.btnExit.Location = new System.Drawing.Point(750, 500);
            this.btnExit.Size = new System.Drawing.Size(100, 40);
            this.btnExit.Text = "Exit";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            this.Controls.Add(this.btnExit);

            // lblVersion
            this.lblVersion.Location = new System.Drawing.Point(20, 570);
            this.lblVersion.Size = new System.Drawing.Size(200, 20);
            this.lblVersion.Text = "v1.00";
            this.Controls.Add(this.lblVersion);

            // logoBox
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
