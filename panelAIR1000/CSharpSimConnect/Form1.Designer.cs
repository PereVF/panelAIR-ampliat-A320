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
            textBox16 = new TextBox();
            textBox20 = new TextBox();
            textBox21 = new TextBox();
            checkBox1 = new CheckBox();
            textBox22 = new TextBox();
            textBox23 = new TextBox();
            closeButton = new Button();
            pbMain = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbMain).BeginInit();
            SuspendLayout();
            //
            // textBox16
            //
            textBox16.BackColor = Color.Black;
            textBox16.BorderStyle = BorderStyle.None;
            textBox16.Font = new Font("Dubai", 24F, FontStyle.Regular, GraphicsUnit.Point);
            textBox16.ForeColor = Color.White;
            textBox16.Location = new Point(645, 190);
            textBox16.Multiline = true;
            textBox16.Name = "textBox16";
            textBox16.Size = new Size(150, 40);
            textBox16.TabIndex = 31;
            textBox16.TextAlign = HorizontalAlignment.Center;
            //
            // textBox20
            //
            textBox20.BackColor = Color.Black;
            textBox20.BorderStyle = BorderStyle.None;
            textBox20.Font = new Font("Dubai", 24F, FontStyle.Regular, GraphicsUnit.Point);
            textBox20.ForeColor = Color.White;
            textBox20.Location = new Point(645, 270);
            textBox20.Multiline = true;
            textBox20.Name = "textBox20";
            textBox20.Size = new Size(150, 40);
            textBox20.TabIndex = 33;
            textBox20.TextAlign = HorizontalAlignment.Center;
            //
            // textBox21
            //
            textBox21.BackColor = Color.Black;
            textBox21.BorderStyle = BorderStyle.None;
            textBox21.Font = new Font("Dubai", 16F, FontStyle.Regular, GraphicsUnit.Point);
            textBox21.ForeColor = Color.Cyan;
            textBox21.Location = new Point(510, 260);
            textBox21.Multiline = true;
            textBox21.Name = "textBox21";
            textBox21.Size = new Size(95, 28);
            textBox21.TabIndex = 34;
            textBox21.TextAlign = HorizontalAlignment.Center;
            //
            // checkBox1
            //
            checkBox1.BackColor = Color.Black;
            checkBox1.Location = new Point(490, 289);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(17, 27);
            checkBox1.TabIndex = 35;
            checkBox1.UseVisualStyleBackColor = true;
            //
            // textBox22
            //
            textBox22.BackColor = Color.Black;
            textBox22.BorderStyle = BorderStyle.None;
            textBox22.Font = new Font("Dubai", 12F, FontStyle.Regular, GraphicsUnit.Point);
            textBox22.ForeColor = Color.Cyan;
            textBox22.Location = new Point(382, 262);
            textBox22.Multiline = true;
            textBox22.Name = "textBox22";
            textBox22.Size = new Size(95, 18);
            textBox22.TabIndex = 36;
            textBox22.TextAlign = HorizontalAlignment.Center;
            //
            // textBox23
            //
            textBox23.BackColor = Color.Black;
            textBox23.BorderStyle = BorderStyle.None;
            textBox23.Font = new Font("Dubai", 12F, FontStyle.Regular, GraphicsUnit.Point);
            textBox23.ForeColor = Color.Cyan;
            textBox23.Location = new Point(382, 289);
            textBox23.Multiline = true;
            textBox23.Name = "textBox23";
            textBox23.Size = new Size(95, 21);
            textBox23.TabIndex = 37;
            textBox23.TextAlign = HorizontalAlignment.Center;
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
            closeButton.Text = "✕  close this cockpit panel";
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
            Controls.Add(closeButton);
            Controls.Add(textBox23);
            Controls.Add(textBox22);
            Controls.Add(textBox16);
            Controls.Add(textBox20);
            Controls.Add(textBox21);
            Controls.Add(checkBox1);
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
        private TextBox textBox16;
        private TextBox textBox20;
        private TextBox textBox21;
        private CheckBox checkBox1;
        private TextBox textBox22;
        private TextBox textBox23;
        private Button closeButton;
        private PictureBox pbMain;
    }
}
