namespace BibtexManager
{
    partial class dlgPreferences
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
            this.pgd = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // pgd
            // 
            this.pgd.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgd.Location = new System.Drawing.Point(12, 12);
            this.pgd.Name = "pgd";
            this.pgd.Size = new System.Drawing.Size(407, 269);
            this.pgd.TabIndex = 6;
            this.pgd.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.pgd_PropertyValueChanged);
            // 
            // dlgOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(431, 293);
            this.Controls.Add(this.pgd);
            this.Name = "dlgOptions";
            this.Text = "Preferences";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.dlgOptions_FormClosing);
            this.Load += new System.EventHandler(this.dlgOptions_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid pgd;
    }
}