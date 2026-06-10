namespace DesktopPet
{
    partial class FormPet
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPet));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.bubbleTimer = new System.Windows.Forms.Timer(this.components);
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.bubblePanel = new System.Windows.Forms.Panel();
			this.bubbleLabel = new System.Windows.Forms.Label();
			this.bubblePanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(40, 40);
			this.imageList1.Tag = "0";
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Cursor = System.Windows.Forms.Cursors.SizeAll;
			this.pictureBox1.ImageLocation = "";
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(40, 40);
			this.pictureBox1.TabIndex = 3;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.PictureBox1_Click);
			this.pictureBox1.DoubleClick += new System.EventHandler(this.pictureBox1_DoubleClick);
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PictureBox1_MouseDown);
			this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PictureBox1_MouseUp);
			// 
			// bubblePanel
			// 
			this.bubblePanel.AutoSize = true;
			this.bubblePanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.bubblePanel.BackColor = System.Drawing.Color.White;
			this.bubblePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.bubblePanel.Controls.Add(this.bubbleLabel);
			this.bubblePanel.Location = new System.Drawing.Point(0, 0);
			this.bubblePanel.MaximumSize = new System.Drawing.Size(250, 200);
			this.bubblePanel.MinimumSize = new System.Drawing.Size(30, 20);
			this.bubblePanel.Name = "bubblePanel";
			this.bubblePanel.Padding = new System.Windows.Forms.Padding(6);
			this.bubblePanel.Size = new System.Drawing.Size(42, 32);
			this.bubblePanel.TabIndex = 4;
			this.bubblePanel.Visible = false;
			// 
			// bubbleLabel
			// 
			this.bubbleLabel.AutoSize = true;
			this.bubbleLabel.BackColor = System.Drawing.Color.White;
			this.bubbleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bubbleLabel.ForeColor = System.Drawing.Color.Black;
			this.bubbleLabel.Location = new System.Drawing.Point(6, 6);
			this.bubbleLabel.MaximumSize = new System.Drawing.Size(230, 180);
			this.bubbleLabel.Name = "bubbleLabel";
			this.bubbleLabel.Size = new System.Drawing.Size(10, 15);
			this.bubbleLabel.TabIndex = 0;
			this.bubbleLabel.Text = "";
			// 
			// bubbleTimer
			// 
			this.bubbleTimer.Interval = 8000;
			this.bubbleTimer.Tick += new System.EventHandler(this.BubbleTimer_Tick);
			// 
			// FormPet
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.BackColor = System.Drawing.Color.Magenta;
			this.ClientSize = new System.Drawing.Size(40, 40);
			this.ControlBox = false;
			this.Controls.Add(this.bubblePanel);
			this.Controls.Add(this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormPet";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Sheep";
			this.TopMost = true;
			this.TransparencyKey = System.Drawing.Color.Magenta;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form2_FormClosed);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form2_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form2_DragEnter);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.bubblePanel.ResumeLayout(false);
			this.bubblePanel.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer bubbleTimer;
        private System.Windows.Forms.Panel bubblePanel;
        private System.Windows.Forms.Label bubbleLabel;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}