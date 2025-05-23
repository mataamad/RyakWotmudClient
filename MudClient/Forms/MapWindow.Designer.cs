namespace MudClient {
    partial class MapWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.SuspendLayout();
            // 
            // MapWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(1300, 1180);
            this.DoubleBuffered = true;
            this.Location = new System.Drawing.Point(1000, 0);
            this.Name = "MapWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Map";
            this.ResizeEnd += new System.EventHandler(this.OnResizeEnd);
            this.SizeChanged += new System.EventHandler(this.MapWindow_SizeChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
            this.ResumeLayout(false);

        }

        #endregion
    }
}