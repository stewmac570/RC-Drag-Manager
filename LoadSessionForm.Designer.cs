namespace RCDragManagerProd
{
    partial class LoadSessionForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView lvSessions;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDelete;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lvSessions = new System.Windows.Forms.ListView();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lvSessions
            // 
            this.lvSessions.FullRowSelect = true;
            this.lvSessions.GridLines = true;
            this.lvSessions.HideSelection = false;
            this.lvSessions.Location = new System.Drawing.Point(50, 50);
            this.lvSessions.MultiSelect = false;
            this.lvSessions.Name = "lvSessions";
            this.lvSessions.Size = new System.Drawing.Size(800, 430);
            this.lvSessions.TabIndex = 0;
            this.lvSessions.UseCompatibleStateImageBehavior = false;
            this.lvSessions.View = System.Windows.Forms.View.Details;

            // btnDelete
            this.btnDelete.Location = new System.Drawing.Point(486, 510);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(90, 40);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // btnLoad
            this.btnLoad.Location = new System.Drawing.Point(644, 510);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(90, 40);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(762, 510);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 40);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // 
            // LoadSessionForm
            // 
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.lvSessions);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnCancel);
            this.Name = "LoadSessionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Load Saved Race Session";
            this.ResumeLayout(false);

        }
    }
}
