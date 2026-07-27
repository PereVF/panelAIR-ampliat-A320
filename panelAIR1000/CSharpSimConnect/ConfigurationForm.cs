using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>
    /// Pantalla de Configuration de panelAIR: Touch Screen, SFSmonitor, SFS Link, TBM 930.
    /// Les 4 targetes es mostren sempre en fila, d'esquerra a dreta.
    /// </summary>
    public class ConfigurationForm : Form
    {
        private Label _touchStatusLbl = null!;
        private Button _touchButton = null!;
        private TextBox _sfsPathBox = null!;
        private ToggleSwitch _sfsAutoLaunchToggle = null!;
        private Label _sfsToggleStateLbl = null!;

        public ConfigurationForm()
        {
            Text = "panelAIR Configuration";
            Size = new Size(2200, 1080);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(242, 244, 246);

            var header = new Label
            {
                Text = "Configuration",
                Font = new Font("Segoe UI", 40f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                AutoSize = true,
                Location = new Point(32, 24)
            };
            Controls.Add(header);

            var cardsRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Location = new Point(32, 120),
                Size = new Size(ClientSize.Width - 64, ClientSize.Height - 220),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(cardsRow);

            cardsRow.Controls.Add(BuildTouchCard());
            cardsRow.Controls.Add(BuildSfsMonitorCard());
            cardsRow.Controls.Add(BuildSfsLinkCard());
            cardsRow.Controls.Add(BuildTbm930Card());

            var resetButton = new Button
            {
                Text = "Reset Setup From Scratch",
                Size = new Size(420, 64),
                Location = new Point(32, ClientSize.Height - 90),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold)
            };
            resetButton.FlatAppearance.BorderSize = 0;
            resetButton.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(this,
                    "This will erase all panelAIR setup preferences and bring back the first-run Welcome screen the next time the app starts. Continue?",
                    "Reset Setup From Scratch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                AppSettings.Instance.ResetToDefaults();
                RefreshFromSettings();
                MessageBox.Show(this, "Setup has been reset. Restart panelAIR to see the first-run experience again.",
                    "Reset Setup From Scratch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(resetButton);

            TouchDetector.TouchDetected += OnTouchDetected;
            FormClosed += (s, e) => TouchDetector.TouchDetected -= OnTouchDetected;
        }

        private void OnTouchDetected()
        {
            if (_touchStatusLbl == null || _touchButton == null) return;
            _touchStatusLbl.Text = "This monitor is configured as the touch screen.";
            _touchButton.Text = "Touch Screen Configured";
        }

        /// <summary>Torna a pintar tots els controls de la pantalla amb l'estat actual de AppSettings.</summary>
        private void RefreshFromSettings()
        {
            _touchStatusLbl.Text = AppSettings.Instance.TouchScreenConfigured
                ? "This monitor is configured as the touch screen." : "";
            _touchButton.Text = AppSettings.Instance.TouchScreenConfigured
                ? "Touch Screen Configured" : "🖐  Make This Screen Touch";

            _sfsPathBox.Text = AppSettings.Instance.SfsMonitorPath;
            _sfsAutoLaunchToggle.Checked = AppSettings.Instance.SfsMonitorAutoLaunch;
            _sfsToggleStateLbl.Text = _sfsAutoLaunchToggle.Checked ? "On" : "Off";
        }

        private static Panel MakeCardShell(int number, string title, string description, out Panel content)
        {
            var card = new Panel
            {
                Size = new Size(520, 960),
                Margin = new Padding(0, 0, 24, 0),
                BackColor = Color.White,
                Padding = new Padding(32)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(217, 223, 229), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var numberBadge = new Label
            {
                Text = number.ToString(),
                Size = new Size(60, 60),
                Location = new Point(32, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold)
            };
            card.Controls.Add(numberBadge);

            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                AutoSize = true,
                Location = new Point(32, 112)
            };
            card.Controls.Add(titleLbl);

            var descLbl = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 19f),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(32, 165),
                Size = new Size(456, 115)
            };
            card.Controls.Add(descLbl);

            content = new Panel
            {
                Location = new Point(32, 295),
                Size = new Size(456, 650)
            };
            card.Controls.Add(content);

            return card;
        }

        private Panel BuildTouchCard()
        {
            var card = MakeCardShell(1, "Touch Screen Setup",
                "Set this monitor as the touch screen used by panelAIR.", out var content);

            _touchButton = new Button
            {
                Text = AppSettings.Instance.TouchScreenConfigured ? "Touch Screen Configured" : "🖐  Make This Screen Touch",
                Size = new Size(456, 90),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold)
            };
            _touchButton.FlatAppearance.BorderSize = 0;
            content.Controls.Add(_touchButton);

            var noteLbl = new Label
            {
                Text = "This will close panelAIR so you can configure the touch screen in Windows. Start panelAIR again afterward.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 15f),
                Location = new Point(0, 104),
                Size = new Size(456, 80)
            };
            content.Controls.Add(noteLbl);

            _touchStatusLbl = new Label
            {
                Text = AppSettings.Instance.TouchScreenConfigured ? "This monitor is configured as the touch screen." : "",
                ForeColor = Color.FromArgb(21, 115, 71),
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 196),
                Size = new Size(456, 80)
            };
            content.Controls.Add(_touchStatusLbl);

            _touchButton.Click += (s, e) => TouchDetector.LaunchWindowsTouchSettingsAndExit(this);

            return card;
        }

        private Panel BuildSfsMonitorCard()
        {
            var card = MakeCardShell(2, "SFSmonitor",
                "Optional: panelAIR can open SFSmonitor automatically when the app starts.", out var content);

            var pathLbl = new Label
            {
                Text = "SFSmonitor location",
                AutoSize = true,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 0)
            };
            content.Controls.Add(pathLbl);

            _sfsPathBox = new TextBox
            {
                Text = AppSettings.Instance.SfsMonitorPath,
                Font = new Font("Segoe UI", 16f),
                Location = new Point(0, 42),
                Size = new Size(310, 40)
            };
            var pathBox = _sfsPathBox;
            content.Controls.Add(pathBox);

            var browseButton = new Button
            {
                Text = "Browse…",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(322, 40),
                Size = new Size(134, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(22, 32, 42)
            };
            content.Controls.Add(browseButton);

            pathBox.TextChanged += (s, e) =>
            {
                AppSettings.Instance.SfsMonitorPath = pathBox.Text;
                AppSettings.Instance.Save();
            };

            browseButton.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Filter = "SFSmonitor executable (*.exe)|*.exe",
                    Title = "Select SFSmonitor.exe"
                };
                string dir = Path.GetDirectoryName(pathBox.Text) ?? "";
                if (Directory.Exists(dir)) dlg.InitialDirectory = dir;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    pathBox.Text = dlg.FileName;
            };

            var helpLbl = new Label
            {
                Text = "Select the SFSmonitor.exe file.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 16f),
                Location = new Point(0, 100),
                AutoSize = true
            };
            content.Controls.Add(helpLbl);

            var switchTitle = new Label
            {
                Text = "Open SFSmonitor automatically",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 200),
                Size = new Size(300, 60)
            };
            content.Controls.Add(switchTitle);

            var switchSub = new Label
            {
                Text = "Turn this on only if you want panelAIR to launch SFSmonitor.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 15f),
                Location = new Point(0, 262),
                Size = new Size(300, 90)
            };
            content.Controls.Add(switchSub);

            _sfsAutoLaunchToggle = new ToggleSwitch
            {
                Checked = AppSettings.Instance.SfsMonitorAutoLaunch,
                Size = new Size(108, 58),
                Location = new Point(330, 200)
            };
            var toggle = _sfsAutoLaunchToggle;
            content.Controls.Add(toggle);

            _sfsToggleStateLbl = new Label
            {
                Text = toggle.Checked ? "On" : "Off",
                Location = new Point(330, 264),
                AutoSize = true,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold)
            };
            var toggleStateLbl = _sfsToggleStateLbl;
            content.Controls.Add(toggleStateLbl);

            toggle.CheckedChanged += (s, e) =>
            {
                AppSettings.Instance.SfsMonitorAutoLaunch = toggle.Checked;
                AppSettings.Instance.Save();
                toggleStateLbl.Text = toggle.Checked ? "On" : "Off";
            };

            return card;
        }

        private Panel BuildSfsLinkCard()
        {
            var card = MakeCardShell(3, "SFS Link",
                "panelAIR will install the latest version of SFS Link into your Microsoft Flight Simulator Community folders. You will be asked to confirm before each folder is modified.",
                out var content);

            var installButton = new Button
            {
                Text = "Install SFS Link",
                Size = new Size(456, 90),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold)
            };
            installButton.FlatAppearance.BorderSize = 0;
            content.Controls.Add(installButton);

            var statusLbl = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(21, 115, 71),
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 104),
                Size = new Size(456, 80)
            };
            content.Controls.Add(statusLbl);

            installButton.Click += async (s, e) =>
            {
                installButton.Enabled = false;
                statusLbl.Text = "Installing…";
                try
                {
                    await SfsLinkInstaller.InstallAsync(this);
                    statusLbl.Text = "Done.";
                }
                finally
                {
                    installButton.Enabled = true;
                }
            };

            return card;
        }

        private Panel BuildTbm930Card()
        {
            var card = MakeCardShell(4, "TBM 930 Setup",
                "To use all panelAIR functions with the TBM 930, install these two add-ons:", out var content);

            AddTbm930Item(content, y: 0,
                title: "TBM 930 LVARs",
                desc: "Adds the required variables for aircraft systems.",
                linkText: "Open download page",
                url: "https://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights");

            AddTbm930Item(content, y: 230,
                title: "MobiFlight WASM Module",
                desc: "Adds the required MSFS background events.",
                linkText: "Open installation guide",
                url: "https://www.mobiflight.com/en/downloads.html");

            return card;
        }

        private static void AddTbm930Item(Panel content, int y, string title, string desc, string linkText, string url)
        {
            var itemTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, y)
            };
            content.Controls.Add(itemTitle);

            var itemDesc = new Label
            {
                Text = desc,
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 16f),
                Location = new Point(0, y + 44),
                Size = new Size(456, 70)
            };
            content.Controls.Add(itemDesc);

            var link = new LinkLabel
            {
                Text = linkText,
                LinkColor = Color.FromArgb(235, 91, 37),
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                Location = new Point(0, y + 120),
                AutoSize = true
            };
            link.LinkClicked += (s, e) =>
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            content.Controls.Add(link);
        }
    }
}
