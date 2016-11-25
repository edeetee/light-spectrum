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
            this.fps_value = new System.Windows.Forms.Label();
            this.fps_title = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
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
            "Debug",
            "Off"});
            this.mode_list.Location = new System.Drawing.Point(55, 137);
            this.mode_list.Name = "mode_list";
            this.mode_list.Size = new System.Drawing.Size(120, 95);
            this.mode_list.TabIndex = 3;
            this.mode_list.SelectedIndexChanged += new System.EventHandler(this.mode_list_SelectedIndexChanged);
            // 
            // fps_value
            // 
            this.fps_value.AutoSize = true;
            this.fps_value.Location = new System.Drawing.Point(52, 73);
            this.fps_value.Name = "fps_value";
            this.fps_value.Size = new System.Drawing.Size(13, 13);
            this.fps_value.TabIndex = 4;
            this.fps_value.Text = "0";
            this.fps_value.Paint += new System.Windows.Forms.PaintEventHandler(this.fps_value_Paint);
            // 
            // fps_title
            // 
            this.fps_title.AutoSize = true;
            this.fps_title.Location = new System.Drawing.Point(52, 41);
            this.fps_title.Name = "fps_title";
            this.fps_title.Size = new System.Drawing.Size(30, 13);
            this.fps_title.TabIndex = 5;
            this.fps_title.Text = "FPS:";
            // 
            // trackBar1
            // 
            this.trackBar1.LargeChange = 1;
            this.trackBar1.Location = new System.Drawing.Point(109, 41);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(163, 45);
            this.trackBar1.TabIndex = 6;
            this.trackBar1.Value = 5;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.fps_title);
            this.Controls.Add(this.fps_value);
            this.Controls.Add(this.mode_list);
            this.Controls.Add(this.mode_label);
            this.Name = "Main";
            this.Text = "LightSpectrum";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label mode_label;
        private System.Windows.Forms.ListBox mode_list;
        private System.Windows.Forms.Label fps_value;
        private System.Windows.Forms.Label fps_title;
        private System.Windows.Forms.TrackBar trackBar1;
    }
}