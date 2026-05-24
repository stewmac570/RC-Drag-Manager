namespace RCDragManagerProd.UI.Forms
{
    partial class LoadSessionForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabSingleClass;
        private System.Windows.Forms.TabPage tabMultiClass;
        private System.Windows.Forms.ListView lvSessions;
        private System.Windows.Forms.ListView lvMultiClass;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDelete;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabSingleClass = new System.Windows.Forms.TabPage();
            this.tabMultiClass = new System.Windows.Forms.TabPage();
            this.lvSessions = new System.Windows.Forms.ListView();
            this.lvMultiClass = new System.Windows.Forms.ListView();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabSingleClass.SuspendLayout();
            this.tabMultiClass.SuspendLayout();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Controls.Add(this.tabSingleClass);
            this.tabControl.Controls.Add(this.tabMultiClass);
            this.tabControl.Location = new System.Drawing.Point(50, 20);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(800, 460);
            this.tabControl.TabIndex = 0;

            // tabSingleClass
            this.tabSingleClass.Controls.Add(this.lvSessions);
            this.tabSingleClass.Location = new System.Drawing.Point(4, 22);
            this.tabSingleClass.Name = "tabSingleClass";
            this.tabSingleClass.Padding = new System.Windows.Forms.Padding(3);
            this.tabSingleClass.Size = new System.Drawing.Size(792, 434);
            this.tabSingleClass.TabIndex = 0;
            this.tabSingleClass.Text = "Single-Class";
            this.tabSingleClass.UseVisualStyleBackColor = true;

            // tabMultiClass
            this.tabMultiClass.Controls.Add(this.lvMultiClass);
            this.tabMultiClass.Location = new System.Drawing.Point(4, 22);
            this.tabMultiClass.Name = "tabMultiClass";
            this.tabMultiClass.Padding = new System.Windows.Forms.Padding(3);
            this.tabMultiClass.Size = new System.Drawing.Size(792, 434);
            this.tabMultiClass.TabIndex = 1;
            this.tabMultiClass.Text = "Multi-Class";
            this.tabMultiClass.UseVisualStyleBackColor = true;

            // lvSessions
            this.lvSessions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSessions.FullRowSelect = true;
            this.lvSessions.GridLines = true;
            this.lvSessions.HideSelection = false;
            this.lvSessions.Location = new System.Drawing.Point(3, 3);
            this.lvSessions.MultiSelect = false;
            this.lvSessions.Name = "lvSessions";
            this.lvSessions.Size = new System.Drawing.Size(786, 428);
            this.lvSessions.TabIndex = 0;
            this.lvSessions.UseCompatibleStateImageBehavior = false;
            this.lvSessions.View = System.Windows.Forms.View.Details;
            this.lvSessions.DoubleClick += new System.EventHandler(this.lvSessions_DoubleClick);

            // lvMultiClass
            this.lvMultiClass.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvMultiClass.FullRowSelect = true;
            this.lvMultiClass.GridLines = true;
            this.lvMultiClass.HideSelection = false;
            this.lvMultiClass.Location = new System.Drawing.Point(3, 3);
            this.lvMultiClass.MultiSelect = false;
            this.lvMultiClass.Name = "lvMultiClass";
            this.lvMultiClass.Size = new System.Drawing.Size(786, 428);
            this.lvMultiClass.TabIndex = 0;
            this.lvMultiClass.UseCompatibleStateImageBehavior = false;
            this.lvMultiClass.View = System.Windows.Forms.View.Details;
            this.lvMultiClass.DoubleClick += new System.EventHandler(this.lvMultiClass_DoubleClick);

            // btnLoad
            this.btnLoad.Location = new System.Drawing.Point(644, 500);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(90, 40);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);

            // btnDelete
            this.btnDelete.Location = new System.Drawing.Point(50, 500);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(90, 40);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(762, 500);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 40);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // LoadSessionForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(900, 560);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnCancel);
            this.Name = "LoadSessionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Load Saved Event";
            this.tabControl.ResumeLayout(false);
            this.tabSingleClass.ResumeLayout(false);
            this.tabMultiClass.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
