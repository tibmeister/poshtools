namespace PowerShellTools.Project
{
    partial class PowerShellGeneralPropertyPageControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCodeSigningCert = new System.Windows.Forms.Button();
            this.txtCodeSigningCert = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnOutputDirectory = new System.Windows.Forms.Button();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkSignOutput = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnCodeSigningCert
            // 
            this.btnCodeSigningCert.Enabled = false;
            this.btnCodeSigningCert.Location = new System.Drawing.Point(373, 76);
            this.btnCodeSigningCert.Name = "btnCodeSigningCert";
            this.btnCodeSigningCert.Size = new System.Drawing.Size(48, 23);
            this.btnCodeSigningCert.TabIndex = 6;
            this.btnCodeSigningCert.Text = "...";
            this.btnCodeSigningCert.UseVisualStyleBackColor = true;
            // 
            // txtCodeSigningCert
            // 
            this.txtCodeSigningCert.Location = new System.Drawing.Point(144, 79);
            this.txtCodeSigningCert.Name = "txtCodeSigningCert";
            this.txtCodeSigningCert.ReadOnly = true;
            this.txtCodeSigningCert.Size = new System.Drawing.Size(223, 20);
            this.txtCodeSigningCert.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Code Signing Certificate";
            // 
            // btnOutputDirectory
            // 
            this.btnOutputDirectory.Location = new System.Drawing.Point(373, 17);
            this.btnOutputDirectory.Name = "btnOutputDirectory";
            this.btnOutputDirectory.Size = new System.Drawing.Size(48, 23);
            this.btnOutputDirectory.TabIndex = 3;
            this.btnOutputDirectory.Text = "...";
            this.btnOutputDirectory.UseVisualStyleBackColor = true;
            this.btnOutputDirectory.Click += new System.EventHandler(this.btnOutputDirectory_Click);
            // 
            // txtOutputDirectory
            // 
            this.txtOutputDirectory.Location = new System.Drawing.Point(144, 17);
            this.txtOutputDirectory.Name = "txtOutputDirectory";
            this.txtOutputDirectory.Size = new System.Drawing.Size(223, 20);
            this.txtOutputDirectory.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Output Directory";
            // 
            // chkSignOutput
            // 
            this.chkSignOutput.AutoSize = true;
            this.chkSignOutput.Location = new System.Drawing.Point(17, 52);
            this.chkSignOutput.Name = "chkSignOutput";
            this.chkSignOutput.Size = new System.Drawing.Size(82, 17);
            this.chkSignOutput.TabIndex = 0;
            this.chkSignOutput.Text = "Sign Output";
            this.chkSignOutput.UseVisualStyleBackColor = true;
            this.chkSignOutput.CheckedChanged += new System.EventHandler(this.chkSignOutput_CheckedChanged);
            // 
            // PowerShellGeneralPropertyPageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnCodeSigningCert);
            this.Controls.Add(this.txtCodeSigningCert);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkSignOutput);
            this.Controls.Add(this.btnOutputDirectory);
            this.Controls.Add(this.txtOutputDirectory);
            this.Name = "PowerShellGeneralPropertyPageControl";
            this.Size = new System.Drawing.Size(443, 117);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkSignOutput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.TextBox txtCodeSigningCert;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnOutputDirectory;
        private System.Windows.Forms.Button btnCodeSigningCert;
    }
}
