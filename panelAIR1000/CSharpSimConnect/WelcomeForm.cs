using System.Drawing;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>Pantalla de benvinguda mostrada només al primer arrencada de panelAIR.</summary>
    public class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "panelAIR";
            Size = new Size(1200, 900);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(21, 32, 43);

            var card = new Panel
            {
                Size = new Size(1000, 760),
                Location = new Point((Width - 1000) / 2 - 8, (Height - 760) / 2 - 20),
                BackColor = Color.White
            };
            Controls.Add(card);

            var eyebrow = new Label
            {
                Text = "FIRST-TIME SETUP",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 91, 37),
                AutoSize = true,
                Location = new Point(64, 56)
            };
            card.Controls.Add(eyebrow);

            var title = new Label
            {
                Text = "Welcome to panelAIR",
                Font = new Font("Segoe UI", 52f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                AutoSize = true,
                Location = new Point(60, 100)
            };
            card.Controls.Add(title);

            var body1 = new Label
            {
                Text = "Before continuing, make sure your panelAIR 4K monitor is connected to your MSFS computer as explained in the Quick Start Guide.",
                Font = new Font("Segoe UI", 22f),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(64, 270),
                Size = new Size(860, 140)
            };
            card.Controls.Add(body1);

            var body2 = new Label
            {
                Text = "The Configuration screen will open next. Please complete step 5, Touch Gestures, to prevent this touch screen from accidentally opening Windows menus. You can reopen Configuration at any time from the CONFIGURATION button.",
                Font = new Font("Segoe UI", 22f),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(64, 430),
                Size = new Size(860, 170)
            };
            card.Controls.Add(body2);

            var continueButton = new Button
            {
                Text = "Continue",
                Size = new Size(280, 80),
                Location = new Point(64, 610),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold)
            };
            continueButton.FlatAppearance.BorderSize = 0;
            continueButton.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            card.Controls.Add(continueButton);
        }
    }
}
