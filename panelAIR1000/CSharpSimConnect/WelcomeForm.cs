using System.Drawing;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>Pantalla de benvinguda mostrada només al primer arrencada de panelAIR.</summary>
    public class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            // Mateix patró que els missatges d'entrada/final de DEMO (FlowLayoutPanel + AutoSize +
            // AutoScaleMode.Dpi): robust a qualsevol DPI, en lloc de Location/Size fixos en píxels
            // dins d'un Form de mida fixa, que és el que feia sortir aquesta finestra desproporcionada.
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "panelAIR";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(40);

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };

            var eyebrow = new Label
            {
                Text = "FIRST-TIME SETUP",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 91, 37),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            flow.Controls.Add(eyebrow);

            var title = new Label
            {
                Text = "Welcome to panelAIR",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };
            flow.Controls.Add(title);

            var bodyFont = new Font("Segoe UI", 11f);
            var bodyColor = Color.FromArgb(102, 114, 127);

            var body1 = new Label
            {
                Text = "Before continuing, make sure your panelAIR 4K monitor is connected to your MSFS computer as explained in the Quick Start Guide.",
                Font = bodyFont,
                ForeColor = bodyColor,
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Margin = new Padding(0, 0, 0, 16)
            };
            flow.Controls.Add(body1);

            var body2 = new Label
            {
                Text = "The Configuration screen will open next. Go through all 6 steps for a quick panelAIR setup on this Windows PC. You can reopen Configuration at any time from the CONFIGURATION button.",
                Font = bodyFont,
                ForeColor = bodyColor,
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Margin = new Padding(0, 0, 0, 24)
            };
            flow.Controls.Add(body2);

            var continueButton = new Button
            {
                Text = "Continue",
                Size = new Size(180, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };
            continueButton.FlatAppearance.BorderSize = 0;
            continueButton.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            flow.Controls.Add(continueButton);

            Controls.Add(flow);
            AcceptButton = continueButton;
        }
    }
}
