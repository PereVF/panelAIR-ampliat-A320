using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SimConnect3
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox21 = new TextBox();
            closeButton = new Button();
            pbMain = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbMain).BeginInit();
            SuspendLayout();
            //
            // textBox21
            //
            textBox21.BackColor = Color.FromArgb(40, 40, 40);
            textBox21.BorderStyle = BorderStyle.None;
            textBox21.Font = new Font("Segoe UI Emoji", 17.5F, FontStyle.Bold, GraphicsUnit.Point);
            textBox21.ForeColor = Color.Cyan;
            textBox21.Location = new Point(510, 260);
            textBox21.Multiline = true;
            textBox21.Name = "textBox21";
            textBox21.Size = new Size(353, 55);
            textBox21.TabIndex = 34;
            textBox21.TextAlign = HorizontalAlignment.Center;
            //
            // closeButton
            //
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.BackColor = Color.DarkRed;
            closeButton.FlatAppearance.BorderSize = 1;
            closeButton.FlatAppearance.BorderColor = Color.White;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.Font = new Font("Segoe UI Emoji", 17.5F, FontStyle.Bold, GraphicsUnit.Point);
            closeButton.ForeColor = Color.White;
            closeButton.Location = new Point(3477, 1100);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(363, 63);
            closeButton.TabIndex = 0;
            closeButton.Text = "✕  EXIT pnanelAIR";
            closeButton.UseVisualStyleBackColor = false;
            closeButton.Click += closeButton_Click;
            //
            // pbMain
            //
            pbMain.BackColor = Color.Black;
            pbMain.Location = new Point(0, 0);
            pbMain.Name = "pbMain";
            pbMain.Size = new Size(3840, 2160);
            pbMain.SizeMode = PictureBoxSizeMode.Normal;
            pbMain.TabIndex = 38;
            pbMain.TabStop = false;
            //
            // frmMain
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(3840, 2160);
            Controls.Add(pbMain);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            Name = "frmMain";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.Manual;
            Text = "SimConnect and C#";
            ((System.ComponentModel.ISupportInitialize)pbMain).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textBox21;
        private Button closeButton;
        private PictureBox pbMain;
    }
}
