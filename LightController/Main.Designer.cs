namespace LightController
{
    partial class Main
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
            this.mode_label = new System.Windows.Forms.Label();
            this.mode_list = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // mode_label
            // 
            this.mode_label.AutoSize = true;
            this.mode_label.Location = new System.Drawing.Point(52, 121);
            this.mode_label.Name = "mode_label";
            this.mode_label.Size = new System.Drawing.Size(34, 13);
            this.mode_label.TabIndex = 2;
            this.mode_label.Text = "Mode";
            this.mode_label.Click += new System.EventHandler(this.label1_Click);
            // 
            // mode_list
            // 
            this.mode_list.FormattingEnabled = true;
            this.mode_list.Items.AddRange(new object[] {
            "Spectrum",
            "Screen",
            "Clock",
            "Off"});
            this.mode_list.Location = new System.Drawing.Point(55, 137);
            this.mode_list.Name = "mode_list";
            this.mode_list.Size = new System.Drawing.Size(120, 95);
            this.mode_list.TabIndex = 3;
            this.mode_list.SelectedIndexChanged += new System.EventHandler(this.mode_list_SelectedIndexChanged);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.mode_list);
            this.Controls.Add(this.mode_label);
            this.Name = "Main";
            this.Text = "Main";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label mode_label;
        private System.Windows.Forms.ListBox mode_list;
    }
}