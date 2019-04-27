namespace FSS
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.btnEditSource = new System.Windows.Forms.Button();
            this.chkBoxStartWhenWindowsStarts = new System.Windows.Forms.CheckBox();
            this.numericUpDownMinutesBeforeStart = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnStartScreenSaver = new System.Windows.Forms.Button();
            this.btnMinimize = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinutesBeforeStart)).BeginInit();
            this.SuspendLayout();
            // 
            // btnEditSource
            // 
            this.btnEditSource.Location = new System.Drawing.Point(12, 271);
            this.btnEditSource.Name = "btnEditSource";
            this.btnEditSource.Size = new System.Drawing.Size(115, 41);
            this.btnEditSource.TabIndex = 0;
            this.btnEditSource.Text = "Edit javascript";
            this.btnEditSource.UseVisualStyleBackColor = true;
            this.btnEditSource.Click += new System.EventHandler(this.btnEditSource_Click);
            // 
            // chkBoxStartWhenWindowsStarts
            // 
            this.chkBoxStartWhenWindowsStarts.AutoSize = true;
            this.chkBoxStartWhenWindowsStarts.Checked = true;
            this.chkBoxStartWhenWindowsStarts.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBoxStartWhenWindowsStarts.Location = new System.Drawing.Point(13, 13);
            this.chkBoxStartWhenWindowsStarts.Name = "chkBoxStartWhenWindowsStarts";
            this.chkBoxStartWhenWindowsStarts.Size = new System.Drawing.Size(639, 21);
            this.chkBoxStartWhenWindowsStarts.TabIndex = 1;
            this.chkBoxStartWhenWindowsStarts.Text = "Start screensaver when Windows starts (if not checked you must start the screensa" +
    "ver manually)";
            this.chkBoxStartWhenWindowsStarts.UseVisualStyleBackColor = true;
            this.chkBoxStartWhenWindowsStarts.CheckedChanged += new System.EventHandler(this.chkBoxStartWhenWindowsStarts_CheckedChanged);
            // 
            // numericUpDownMinutesBeforeStart
            // 
            this.numericUpDownMinutesBeforeStart.Location = new System.Drawing.Point(193, 75);
            this.numericUpDownMinutesBeforeStart.Name = "numericUpDownMinutesBeforeStart";
            this.numericUpDownMinutesBeforeStart.Size = new System.Drawing.Size(120, 22);
            this.numericUpDownMinutesBeforeStart.TabIndex = 2;
            this.numericUpDownMinutesBeforeStart.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownMinutesBeforeStart.ValueChanged += new System.EventHandler(this.numericUpDownMinutesBeforeStart_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(175, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Screensaver will start after";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(319, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "minutes of inactivity";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 251);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(566, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "Build your own screensaver! Click here to edit the javascript file running the sc" +
    "reensaver.";
            // 
            // btnStartScreenSaver
            // 
            this.btnStartScreenSaver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartScreenSaver.Location = new System.Drawing.Point(648, 372);
            this.btnStartScreenSaver.Name = "btnStartScreenSaver";
            this.btnStartScreenSaver.Size = new System.Drawing.Size(140, 41);
            this.btnStartScreenSaver.TabIndex = 6;
            this.btnStartScreenSaver.Text = "Boot the matrix";
            this.btnStartScreenSaver.UseVisualStyleBackColor = true;
            this.btnStartScreenSaver.Click += new System.EventHandler(this.btnStartScreenSaver_Click);
            // 
            // btnMinimize
            // 
            this.btnMinimize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMinimize.Location = new System.Drawing.Point(12, 372);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Size = new System.Drawing.Size(140, 41);
            this.btnMinimize.TabIndex = 7;
            this.btnMinimize.Text = "Minimize";
            this.btnMinimize.UseVisualStyleBackColor = true;
            this.btnMinimize.Click += new System.EventHandler(this.btnMinimize_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbout.Location = new System.Drawing.Point(707, 12);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(81, 41);
            this.btnAbout.TabIndex = 8;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(800, 425);
            this.ControlBox = false;
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.btnMinimize);
            this.Controls.Add(this.btnStartScreenSaver);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDownMinutesBeforeStart);
            this.Controls.Add(this.chkBoxStartWhenWindowsStarts);
            this.Controls.Add(this.btnEditSource);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.Text = "FSS Settings";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMinutesBeforeStart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnEditSource;
        private System.Windows.Forms.CheckBox chkBoxStartWhenWindowsStarts;
        private System.Windows.Forms.NumericUpDown numericUpDownMinutesBeforeStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnStartScreenSaver;
        private System.Windows.Forms.Button btnMinimize;
        private System.Windows.Forms.Button btnAbout;
    }
}