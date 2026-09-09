using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>
    /// Pantalla de Configuration de panelAIR: Screen Position, Touch Screen, SFSmonitor, SFS Link,
    /// TBM 930, Touch Gestures. Les 6 targetes es mostren sempre en fila, d'esquerra a dreta.
    /// </summary>
    public class ConfigurationForm : Form
    {
        private Label _touchStatusLbl = null!;
        private Button _touchButton = null!;
        private TextBox _sfsPathBox = null!;
        private Label _sfsHelpLbl = null!;
        private ToggleSwitch _sfsAutoLaunchToggle = null!;
        private Label _sfsToggleStateLbl = null!;
        private readonly List<Control> _sfsQuestionControls = new();
        private readonly List<Control> _sfsMainControls = new();
        private Button _sfsInstallButton = null!;
        private Label _sfsInstallStatusLbl = null!;
        private ToggleSwitch _edgeSwipeToggle = null!;
        private Label _edgeSwipeStatusLbl = null!;
        private bool _edgeSwipeUpdating = false;
        private Panel _resetOverlayBackdrop = null!;
        private Panel _resetOverlayCard = null!;
        private Label _resetOverlayMsgLbl = null!;
        private Button _resetOverlayPrimaryBtn = null!;
        private Button _resetOverlaySecondaryBtn = null!;
        private bool _resetAwaitingConfirm = false;

        // Overlay genèric reutilitzable per a qualsevol confirmació/avís que abans feia servir
        // MessageBox.Show (Touch Screen, SFS Link...): un MessageBox pot quedar amagat darrere
        // de MSFS en Full Screen i penjar l'app en aparença; aquest overlay viu dins la mateixa
        // finestra, igual que el de Reset, i sempre a la part de dalt perquè mai coincideixi amb
        // l'AUX en coordenades de pantalla.
        private Panel _dialogOverlayBackdrop = null!;
        private Panel _dialogOverlayCard = null!;
        private Label _dialogOverlayMsgLbl = null!;
        private Button _dialogOverlayPrimaryBtn = null!;
        private Button _dialogOverlaySecondaryBtn = null!;
        private TaskCompletionSource<bool>? _dialogTcs;
        private Form? _popoutGuideForm;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // El navegador per defecte sovint obre finestres noves al monitor 4K de panelAIR (perquè és
        // on viu aquesta pantalla de Configuration), no al monitor principal de l'usuari. Aquí es
        // llança l'enllaç normalment i, NOMÉS si el procés que engega és realment una finestra nova
        // (no una pestanya en una finestra de navegador ja oberta, cas en què no toquem res), es
        // mou al monitor principal.
        private static void OpenUrlOnPrimaryMonitor(string url)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                if (proc == null) return;

                var primary = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);

                Task.Run(async () =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(150);
                        try
                        {
                            if (proc.HasExited) return;
                            proc.Refresh();
                            IntPtr h = proc.MainWindowHandle;
                            if (h != IntPtr.Zero)
                            {
                                int width = Math.Min(1400, primary.Width - 100);
                                int height = Math.Min(900, primary.Height - 100);
                                int x = primary.Left + (primary.Width - width) / 2;
                                int y = primary.Top + (primary.Height - height) / 2;
                                SetWindowPos(h, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
                                return;
                            }
                        }
                        catch { return; }
                    }
                });
            }
            catch { }
        }

        public ConfigurationForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "panelAIR Configuration";
            Size = new Size(3840, 1375);
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

            cardsRow.Controls.Add(BuildScreenPositionCard());
            cardsRow.Controls.Add(BuildTouchCard());
            cardsRow.Controls.Add(BuildSfsMonitorCard());
            cardsRow.Controls.Add(BuildSfsLinkCard());
            cardsRow.Controls.Add(BuildEdgeSwipeCard());
            cardsRow.Controls.Add(BuildPopoutCard());
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
                _resetAwaitingConfirm = true;
                _resetOverlayMsgLbl.Text = "This will erase all setup preferences and show the Welcome screen again on next start. Continue?";
                _resetOverlaySecondaryBtn.Visible = true;
                _resetOverlayPrimaryBtn.Text = "Yes, Reset";
                _resetOverlayBackdrop.Visible = true;
                _resetOverlayBackdrop.BringToFront();
            };
            Controls.Add(resetButton);

            // --- Overlay de confirmació del Reset, dins la mateixa finestra (mai un MessageBox/finestra
            // nova: MSFS en mode Full Screen pot amagar diàlegs nous, però aquesta finestra ja se sap
            // que es veu bé amb MSFS obert, així que hi integrem la confirmació directament). ---
            _resetOverlayBackdrop = new Panel
            {
                BackColor = Color.FromArgb(242, 244, 246),
                Dock = DockStyle.Fill,
                Visible = false
            };
            Controls.Add(_resetOverlayBackdrop);

            _resetOverlayCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(760, 320)
            };
            _resetOverlayCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(217, 223, 229), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _resetOverlayCard.Width - 1, _resetOverlayCard.Height - 1);
            };
            _resetOverlayBackdrop.Controls.Add(_resetOverlayCard);
            // Situada a la part de dalt (no centrada verticalment) perquè mai coincideixi, en
            // coordenades de pantalla, amb els botons de la pantalla AUX (que viu més avall).
            void CenterResetCard()
            {
                _resetOverlayCard.Location = new Point(
                    (_resetOverlayBackdrop.Width - _resetOverlayCard.Width) / 2,
                    80);
            }
            CenterResetCard();
            _resetOverlayBackdrop.Resize += (s, e) => CenterResetCard();

            _resetOverlayMsgLbl = new Label
            {
                Font = new Font("Segoe UI", 20f),
                ForeColor = Color.FromArgb(22, 32, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 40),
                Size = new Size(_resetOverlayCard.Width - 80, 160)
            };
            _resetOverlayCard.Controls.Add(_resetOverlayMsgLbl);

            _resetOverlaySecondaryBtn = new Button
            {
                Text = "No",
                Size = new Size(200, 64),
                Location = new Point(120, 220),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 230),
                ForeColor = Color.FromArgb(22, 32, 42),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _resetOverlaySecondaryBtn.FlatAppearance.BorderSize = 2;
            _resetOverlaySecondaryBtn.FlatAppearance.BorderColor = Color.FromArgb(180, 186, 194);
            _resetOverlaySecondaryBtn.Click += (s, e) => _resetOverlayBackdrop.Visible = false;
            _resetOverlayCard.Controls.Add(_resetOverlaySecondaryBtn);

            _resetOverlayPrimaryBtn = new Button
            {
                Text = "Yes, Reset",
                Size = new Size(200, 64),
                Location = new Point(440, 220),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _resetOverlayPrimaryBtn.FlatAppearance.BorderSize = 0;
            _resetOverlayPrimaryBtn.Click += (s, e) =>
            {
                if (_resetAwaitingConfirm)
                {
                    _resetAwaitingConfirm = false;
                    AppSettings.Instance.ResetToDefaults();
                    RefreshFromSettings();
                    _resetOverlayMsgLbl.Text = "Setup reset. Restart panelAIR to see the first-run experience again.";
                    _resetOverlaySecondaryBtn.Visible = false;
                    _resetOverlayPrimaryBtn.Text = "OK";
                    _resetOverlayPrimaryBtn.Location = new Point((_resetOverlayCard.Width - _resetOverlayPrimaryBtn.Width) / 2, 220);
                }
                else
                {
                    _resetOverlayBackdrop.Visible = false;
                    _resetOverlayPrimaryBtn.Location = new Point(440, 220);
                }
            };
            _resetOverlayCard.Controls.Add(_resetOverlayPrimaryBtn);

            // --- Overlay genèric (Touch Screen, SFS Link...) — mateix disseny que el de Reset. ---
            _dialogOverlayBackdrop = new Panel
            {
                BackColor = Color.FromArgb(242, 244, 246),
                Dock = DockStyle.Fill,
                Visible = false
            };
            Controls.Add(_dialogOverlayBackdrop);

            _dialogOverlayCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(1000, 420)
            };
            _dialogOverlayCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(217, 223, 229), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _dialogOverlayCard.Width - 1, _dialogOverlayCard.Height - 1);
            };
            _dialogOverlayBackdrop.Controls.Add(_dialogOverlayCard);
            void CenterDialogCard()
            {
                _dialogOverlayCard.Location = new Point(
                    (_dialogOverlayBackdrop.Width - _dialogOverlayCard.Width) / 2,
                    80);
            }
            CenterDialogCard();
            _dialogOverlayBackdrop.Resize += (s, e) => CenterDialogCard();

            _dialogOverlayMsgLbl = new Label
            {
                Font = new Font("Segoe UI", 16f),
                ForeColor = Color.FromArgb(22, 32, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 30),
                Size = new Size(_dialogOverlayCard.Width - 80, 300)
            };
            _dialogOverlayCard.Controls.Add(_dialogOverlayMsgLbl);

            _dialogOverlaySecondaryBtn = new Button
            {
                Size = new Size(220, 64),
                Location = new Point(280, 340),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 230),
                ForeColor = Color.FromArgb(22, 32, 42),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _dialogOverlaySecondaryBtn.FlatAppearance.BorderSize = 2;
            _dialogOverlaySecondaryBtn.FlatAppearance.BorderColor = Color.FromArgb(180, 186, 194);
            _dialogOverlaySecondaryBtn.Click += (s, e) =>
            {
                _dialogOverlayBackdrop.Visible = false;
                _dialogTcs?.TrySetResult(false);
            };
            _dialogOverlayCard.Controls.Add(_dialogOverlaySecondaryBtn);

            _dialogOverlayPrimaryBtn = new Button
            {
                Size = new Size(220, 64),
                Location = new Point(520, 340),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _dialogOverlayPrimaryBtn.FlatAppearance.BorderSize = 0;
            _dialogOverlayPrimaryBtn.Click += (s, e) =>
            {
                _dialogOverlayBackdrop.Visible = false;
                _dialogTcs?.TrySetResult(true);
            };
            _dialogOverlayCard.Controls.Add(_dialogOverlayPrimaryBtn);

            TouchDetector.TouchDetected += OnTouchDetected;
            FormClosed += (s, e) => TouchDetector.TouchDetected -= OnTouchDetected;
        }

        // Finestra independent de veritat (no un overlay dins de ConfigurationForm): en tenir
        // Owner=this, Windows la manté sempre per davant de la finestra de Configuració mentre
        // estigui oberta, però en tancar-la la de Configuració es queda oberta tal qual, amb les
        // 7 targetes — exactament com una finestra normal de Windows es comporta amb el seu pare.
        private void ShowPopoutGuide()
        {
            if (_popoutGuideForm != null && !_popoutGuideForm.IsDisposed)
            {
                _popoutGuideForm.Activate();
                return;
            }

            // La guia s'obre sobre la pantalla 4K de panelAIR (mateixa detecció que la resta de
            // l'app), amb un 80% de la seva alçada i un 40% de la seva amplada — dona un format
            // vertical còmode de llegir i, en ser més ampla que abans, deixa que el text i la
            // captura aprofitin més espai horitzontal (el seu ample ja es calcula relatiu a
            // guideForm.ClientSize.Width, així que s'adapta sol).
            var screen4k = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                ?? Screen.AllScreens.FirstOrDefault(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                ?? Screen.PrimaryScreen
                ?? Screen.AllScreens[0];

            int guideWidth = (int)(screen4k.Bounds.Width * 0.50);
            int guideHeight = (int)(screen4k.Bounds.Height * 0.90);
            int guideX = screen4k.Bounds.Left + (screen4k.Bounds.Width - guideWidth) / 2;
            int guideY = screen4k.Bounds.Top + (screen4k.Bounds.Height - guideHeight) / 2;

            var guideForm = new Form
            {
                AutoScaleMode = AutoScaleMode.None,
                Text = "POPM Setup Guide",
                StartPosition = FormStartPosition.Manual,
                Location = new Point(guideX, guideY),
                Size = new Size(guideWidth, guideHeight),
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // Panell amb scroll: si el text + la imatge no hi caben sencers, es pot baixar en
            // comptes de tallar-se — mai més perdre contingut per manca d'espai.
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            guideForm.Controls.Add(scrollPanel);

            int textWidth = guideForm.ClientSize.Width - 64;

            var introLbl = new Label
            {
                Text = "First, install POPM using the instructions on its download page.\r\n" +
                       "Once it's installed, set up each aircraft like this:",
                Font = new Font("Segoe UI", 24f),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, 32),
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0)
            };
            scrollPanel.Controls.Add(introLbl);

            // Icona "+" retallada directament de la captura de POPM: es posa al costat del pas 1
            // perquè es vegi exactament quin botó prémer, sense haver-la de descriure amb paraules.
            const int iconSize = 44;
            var step1Lbl = new Label
            {
                Text = "1.  New profile — press \"+\", name the aircraft, then Accept.",
                Font = new Font("Segoe UI", 24f),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, introLbl.Bottom + 24),
                AutoSize = true,
                MaximumSize = new Size(textWidth - iconSize - 16, 0)
            };
            scrollPanel.Controls.Add(step1Lbl);

            var step1Icon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(step1Lbl.Right + 16, step1Lbl.Top),
                Size = new Size(iconSize, iconSize)
            };
            try
            {
                using var si = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("panelAIR1000.Resources.popm_new_profile_icon.png");
                if (si != null) step1Icon.Image = Image.FromStream(si);
            }
            catch { }
            scrollPanel.Controls.Add(step1Icon);

            int afterStep1Y = Math.Max(step1Lbl.Bottom, step1Icon.Bottom) + 24;

            var step2Lbl = new Label
            {
                Text = "2.  Add panels — one for PFD, one for MFD (plus TSC1 and TSC2 for the TBM 930). Paste the Top/Left/Width/Height values we provide — they're the same for any aircraft.",
                Font = new Font("Segoe UI", 24f),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, afterStep1Y),
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0)
            };
            scrollPanel.Controls.Add(step2Lbl);

            var step3Lbl = new Label
            {
                Text = "3.  One panel at a time: press \"Identify\", click that instrument's screen inside MSFS (a colored circle appears), then press the green target icon at the top to confirm. Repeat for each screen — 2 times for a regular G1000 aircraft (PFD, MFD), 4 times for the TBM 930 (PFD, MFD, TSC1, TSC2).",
                Font = new Font("Segoe UI", 24f),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, step2Lbl.Bottom + 24),
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0)
            };
            scrollPanel.Controls.Add(step3Lbl);

            // Captura un 50% més gran que abans (600 -> 900 d'alçada màxima).
            var screenshotBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(32, step3Lbl.Bottom + 32),
                Size = new Size(textWidth, 900)
            };
            try
            {
                using var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("panelAIR1000.Resources.popm_setup_guide_screenshot.png");
                if (s != null) screenshotBox.Image = Image.FromStream(s);
            }
            catch { }
            scrollPanel.Controls.Add(screenshotBox);

            var oneProfileNoteLbl = new Label
            {
                Text = "You'll need one profile in POPM for every aircraft you want to fly — paste the same G1000 values below into each one.",
                Font = new Font("Segoe UI", 20f),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(32, screenshotBox.Bottom + 32),
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0)
            };
            scrollPanel.Controls.Add(oneProfileNoteLbl);

            var coordsTitleLbl = new Label
            {
                Text = "These are your G1000 coordinates",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, oneProfileNoteLbl.Bottom + 24),
                AutoSize = true
            };
            scrollPanel.Controls.Add(coordsTitleLbl);

            // Valors de referència capturats al propi equip de Pere, amb la pantalla 4K a (0, 1440)
            // (veure project_dpi_issue / project_popm_integration) — es desplacen pel delta entre
            // aquella posició i la posició real detectada de la pantalla 4K d'aquest client. Width i
            // Height no canvien mai (mateixa resolució 3840x2160 sempre, és un requisit de hardware).
            const int refScreenLeft = 0, refScreenTop = 1440;
            int deltaX = screen4k.Bounds.Left - refScreenLeft;
            int deltaY = screen4k.Bounds.Top - refScreenTop;

            var pfd = (Top: 1875 + deltaY, Left: 180 + deltaX, Width: 1640, Height: 1025);
            var mfd = (Top: 1875 + deltaY, Left: 2020 + deltaX, Width: 1640, Height: 1025);

            var coordsGrid = new DataGridView
            {
                Location = new Point(32, coordsTitleLbl.Bottom + 16),
                Size = new Size(700, 116),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(217, 223, 229),
                Font = new Font("Segoe UI", 16f),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate = { Height = 40 }
            };
            coordsGrid.Columns.Add("panel", "Panel");
            coordsGrid.Columns.Add("top", "Top");
            coordsGrid.Columns.Add("left", "Left");
            coordsGrid.Columns.Add("width", "Width");
            coordsGrid.Columns.Add("height", "Height");
            foreach (DataGridViewColumn col in coordsGrid.Columns) col.Width = 130;
            coordsGrid.Rows.Add("PFD", pfd.Top, pfd.Left, pfd.Width, pfd.Height);
            coordsGrid.Rows.Add("MFD", mfd.Top, mfd.Left, mfd.Width, mfd.Height);
            scrollPanel.Controls.Add(coordsGrid);

            _popoutGuideForm = guideForm;
            guideForm.Show(this);
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
            UpdateSfsMonitorCardState();

            RefreshEdgeSwipeState();
        }

        /// <summary>Mostra l'overlay genèric amb dos botons i espera que l'usuari en premi un.</summary>
        internal Task<bool> ShowConfirmDialogAsync(string message, string yesText = "Yes", string noText = "No")
        {
            _dialogTcs = new TaskCompletionSource<bool>();
            _dialogOverlayMsgLbl.Text = message;
            _dialogOverlaySecondaryBtn.Text = noText;
            _dialogOverlaySecondaryBtn.Visible = true;
            _dialogOverlayPrimaryBtn.Text = yesText;
            _dialogOverlayBackdrop.Visible = true;
            _dialogOverlayBackdrop.BringToFront();
            return _dialogTcs.Task;
        }

        /// <summary>Mostra l'overlay genèric amb un únic botó (equivalent a un MessageBox informatiu).</summary>
        internal Task ShowInfoDialogAsync(string message, string okText = "OK")
        {
            _dialogTcs = new TaskCompletionSource<bool>();
            _dialogOverlayMsgLbl.Text = message;
            _dialogOverlaySecondaryBtn.Visible = false;
            _dialogOverlayPrimaryBtn.Text = okText;
            _dialogOverlayBackdrop.Visible = true;
            _dialogOverlayBackdrop.BringToFront();
            return _dialogTcs.Task;
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

            // Si la descripció és buida, no es crea cap etiqueta — encara que no tingui text,
            // una etiqueta buida igualment pinta el seu propi fons i, per l'ordre de z-order de
            // WinForms (el primer control afegit queda AL DAVANT, no al darrere), taparia la part
            // de dalt de qualsevol cosa que hi hagi després dins "content" al mateix tros de pantalla.
            if (!string.IsNullOrEmpty(description))
            {
                var descLbl = new Label
                {
                    Text = description,
                    Font = new Font("Segoe UI", 19f),
                    ForeColor = Color.FromArgb(102, 114, 127),
                    Location = new Point(32, 165),
                    Size = new Size(456, 175)
                };
                card.Controls.Add(descLbl);
            }

            content = new Panel
            {
                Location = new Point(32, 355),
                Size = new Size(456, 590)
            };
            card.Controls.Add(content);

            return card;
        }

        private Panel BuildScreenPositionCard()
        {
            var card = MakeCardShell(1, "Screen Position",
                "Tell Windows where this screen sits, so dragging G1000/G3000 pop-outs here feels natural.",
                out var content);

            var openButton = new Button
            {
                Text = "Open Display Settings",
                Size = new Size(456, 90),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold)
            };
            openButton.FlatAppearance.BorderSize = 0;
            content.Controls.Add(openButton);

            var dpiNoteLbl = new Label
            {
                Text = "Recommended: while configuring this screen, set Windows text size to 200% (Settings → Display → Scale) — pop-up windows and dialog titles follow this setting.",
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(0, 104),
                Size = new Size(456, 110)
            };
            content.Controls.Add(dpiNoteLbl);

            var statusLbl = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(21, 115, 71),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                Location = new Point(0, 224),
                Size = new Size(456, 220)
            };
            content.Controls.Add(statusLbl);

            openButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "ms-settings:display", UseShellExecute = true });
                    statusLbl.ForeColor = Color.FromArgb(21, 115, 71);
                    statusLbl.Text = "Drag the box for your new panelAIR screen (3840×2160) to match where it really sits next to your main screen. Click “Identify” if you're not sure which one it is.";
                }
                catch
                {
                    statusLbl.ForeColor = Color.FromArgb(153, 27, 27);
                    statusLbl.Text = "Could not open Display Settings.";
                }
            };

            return card;
        }

        private Panel BuildTouchCard()
        {
            var card = MakeCardShell(2, "Touch Screen Setup",
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

            _touchButton.Click += async (s, e) =>
            {
                bool ok = await ShowConfirmDialogAsync(
                    "panelAIR will now close. You'll see a Windows configuration menu on the main Windows screen. Configure your touch screen, then start panelAIR again.",
                    "Continue", "Cancel");
                if (!ok) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:::{80F3F1D5-FECA-45F3-BC32-752C152E456E}",
                    UseShellExecute = true
                });
                Application.Exit();
            };

            return card;
        }

        private Panel BuildSfsMonitorCard()
        {
            var card = MakeCardShell(3, "SFSmonitor",
                "panelAIR can automatically launch SFSmonitor, which talks to your rotAIR hardware.",
                out var content);

            // --- Pregunta inicial: només es mostra si encara no hi ha una ruta vàlida coneguda ---
            var questionLbl = new Label
            {
                Text = "Is SFSmonitor already installed on this computer?",
                Font = new Font("Segoe UI", 19f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(456, 90)
            };
            content.Controls.Add(questionLbl);
            _sfsQuestionControls.Add(questionLbl);

            var yesBtn = new Button
            {
                Text = "Yes, it's already installed",
                Size = new Size(456, 64),
                Location = new Point(0, 100),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 230),
                ForeColor = Color.FromArgb(22, 32, 42),
                Font = new Font("Segoe UI", 16f, FontStyle.Bold)
            };
            yesBtn.FlatAppearance.BorderSize = 2;
            yesBtn.FlatAppearance.BorderColor = Color.FromArgb(180, 186, 194);
            yesBtn.Click += (s, e) => ShowSfsMainPanel();
            content.Controls.Add(yesBtn);
            _sfsQuestionControls.Add(yesBtn);

            _sfsInstallButton = new Button
            {
                Text = "No, install it for me",
                Size = new Size(456, 64),
                Location = new Point(0, 176),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold)
            };
            _sfsInstallButton.FlatAppearance.BorderSize = 0;
            _sfsInstallButton.Click += SfsInstallButton_Click;
            content.Controls.Add(_sfsInstallButton);
            _sfsQuestionControls.Add(_sfsInstallButton);

            _sfsInstallStatusLbl = new Label
            {
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(0, 254),
                Size = new Size(456, 80)
            };
            content.Controls.Add(_sfsInstallStatusLbl);
            _sfsQuestionControls.Add(_sfsInstallStatusLbl);

            // --- UI normal: ruta ja coneguda (trobada per navegació o acabada d'instal·lar) ---
            var pathLbl = new Label
            {
                Text = "SFSmonitor location",
                AutoSize = true,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 0)
            };
            content.Controls.Add(pathLbl);
            _sfsMainControls.Add(pathLbl);

            _sfsPathBox = new TextBox
            {
                Text = AppSettings.Instance.SfsMonitorPath,
                Font = new Font("Segoe UI", 16f),
                Location = new Point(0, 42),
                Size = new Size(310, 40)
            };
            var pathBox = _sfsPathBox;
            content.Controls.Add(pathBox);
            _sfsMainControls.Add(pathBox);

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
            _sfsMainControls.Add(browseButton);

            pathBox.TextChanged += (s, e) =>
            {
                AppSettings.Instance.SfsMonitorPath = pathBox.Text;
                AppSettings.Instance.Save();
                _sfsHelpLbl.Visible = string.IsNullOrWhiteSpace(pathBox.Text);
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

            _sfsHelpLbl = new Label
            {
                Text = "Select the SFSmonitor.exe file.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 16f),
                Location = new Point(0, 100),
                AutoSize = true
            };
            content.Controls.Add(_sfsHelpLbl);
            _sfsMainControls.Add(_sfsHelpLbl);

            var switchTitle = new Label
            {
                Text = "Open SFSmonitor automatically",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 200),
                Size = new Size(300, 60)
            };
            content.Controls.Add(switchTitle);
            _sfsMainControls.Add(switchTitle);

            var switchSub = new Label
            {
                Text = "Turn this on only if you want panelAIR to launch SFSmonitor.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 15f),
                Location = new Point(0, 262),
                Size = new Size(300, 90)
            };
            content.Controls.Add(switchSub);
            _sfsMainControls.Add(switchSub);

            _sfsAutoLaunchToggle = new ToggleSwitch
            {
                Checked = AppSettings.Instance.SfsMonitorAutoLaunch,
                Size = new Size(108, 58),
                Location = new Point(330, 200)
            };
            var toggle = _sfsAutoLaunchToggle;
            content.Controls.Add(toggle);
            _sfsMainControls.Add(toggle);

            _sfsToggleStateLbl = new Label
            {
                Text = toggle.Checked ? "On" : "Off",
                Location = new Point(330, 264),
                AutoSize = true,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold)
            };
            var toggleStateLbl = _sfsToggleStateLbl;
            content.Controls.Add(toggleStateLbl);
            _sfsMainControls.Add(toggleStateLbl);

            toggle.CheckedChanged += (s, e) =>
            {
                AppSettings.Instance.SfsMonitorAutoLaunch = toggle.Checked;
                AppSettings.Instance.Save();
                toggleStateLbl.Text = toggle.Checked ? "On" : "Off";
            };

            UpdateSfsMonitorCardState();

            return card;
        }

        private void ShowSfsMainPanel()
        {
            foreach (var c in _sfsQuestionControls) c.Visible = false;
            foreach (var c in _sfsMainControls) c.Visible = true;
            _sfsHelpLbl.Visible = string.IsNullOrWhiteSpace(_sfsPathBox.Text);
        }

        /// <summary>Mostra la pregunta inicial si encara no hi ha cap ruta vàlida coneguda de SFSmonitor.exe.</summary>
        private void UpdateSfsMonitorCardState()
        {
            bool hasValidPath = !string.IsNullOrWhiteSpace(AppSettings.Instance.SfsMonitorPath)
                && File.Exists(AppSettings.Instance.SfsMonitorPath);

            if (hasValidPath)
            {
                ShowSfsMainPanel();
            }
            else
            {
                foreach (var c in _sfsMainControls) c.Visible = false;
                foreach (var c in _sfsQuestionControls) c.Visible = true;
                _sfsInstallStatusLbl.Text = "";
            }
        }

        private async void SfsInstallButton_Click(object? sender, EventArgs e)
        {
            _sfsInstallButton.Enabled = false;
            _sfsInstallStatusLbl.ForeColor = Color.FromArgb(102, 114, 127);
            _sfsInstallStatusLbl.Text = "Downloading and installing SFSmonitor...";

            string? installedExePath;
            try
            {
                installedExePath = await SfsMonitorInstaller.InstallAsync();
            }
            catch (Exception ex)
            {
                _sfsInstallStatusLbl.ForeColor = Color.FromArgb(153, 27, 27);
                _sfsInstallStatusLbl.Text = "Could not install SFSmonitor: " + ex.Message;
                _sfsInstallButton.Enabled = true;
                return;
            }

            if (installedExePath == null)
            {
                _sfsInstallStatusLbl.ForeColor = Color.FromArgb(153, 27, 27);
                _sfsInstallStatusLbl.Text = "Could not find SFSmonitor.exe inside the downloaded package.";
                _sfsInstallButton.Enabled = true;
                return;
            }

            AppSettings.Instance.SfsMonitorPath = installedExePath;
            AppSettings.Instance.Save();
            _sfsPathBox.Text = installedExePath;

            _sfsInstallButton.Enabled = true;
            ShowSfsMainPanel();
        }

        private Panel BuildSfsLinkCard()
        {
            var card = MakeCardShell(4, "SFS Link",
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
            var card = MakeCardShell(7, "TBM 930 Setup",
                "Only if you fly the TBM 930 in MSFS2020: install this add-on. Not needed for MSFS2024.", out var content);

            AddTbm930Item(content, y: 0,
                title: "TBM 930 LVARs",
                desc: "Adds the required variables for aircraft systems.",
                linkText: "Open download page",
                url: "https://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights");

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

        private Panel BuildEdgeSwipeCard()
        {
            var card = MakeCardShell(5, "Disable Windows Gestures",
                "This monitor can accidentally open Windows menus when swiping from the edge of the screen (e.g. using the rudder trim wheel).",
                out var content);

            var switchTitle = new Label
            {
                Text = "Disable Windows edge-swipe gesture",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(456, 60)
            };
            content.Controls.Add(switchTitle);

            var switchSub = new Label
            {
                Text = "Requires administrator confirmation the first time. Can be reverted here at any time.",
                ForeColor = Color.FromArgb(102, 114, 127),
                Font = new Font("Segoe UI", 15f),
                Location = new Point(0, 62),
                Size = new Size(456, 90)
            };
            content.Controls.Add(switchSub);

            _edgeSwipeToggle = new ToggleSwitch
            {
                Size = new Size(108, 58),
                Location = new Point(0, 160)
            };
            content.Controls.Add(_edgeSwipeToggle);

            _edgeSwipeStatusLbl = new Label
            {
                Location = new Point(0, 226),
                Size = new Size(456, 60),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            content.Controls.Add(_edgeSwipeStatusLbl);

            _edgeSwipeToggle.CheckedChanged += async (s, e) =>
            {
                if (_edgeSwipeUpdating) return;
                bool wantDisabled = _edgeSwipeToggle.Checked;
                _edgeSwipeToggle.Enabled = false;
                _edgeSwipeStatusLbl.ForeColor = Color.FromArgb(102, 114, 127);
                _edgeSwipeStatusLbl.Text = "Applying... Windows may ask for administrator confirmation.";

                bool ok = await Task.Run(() => wantDisabled ? EdgeSwipeConfig.Disable() : EdgeSwipeConfig.Enable());
                if (ok) EdgeSwipeConfig.ApplyImmediately();

                RefreshEdgeSwipeState();
                _edgeSwipeToggle.Enabled = true;
                if (!ok)
                {
                    _edgeSwipeStatusLbl.ForeColor = Color.FromArgb(153, 27, 27);
                    _edgeSwipeStatusLbl.Text = "Could not apply the change.";
                }
            };

            RefreshEdgeSwipeState();

            return card;
        }

        private Panel BuildPopoutCard()
        {
            // Descripció buida a propòsit: tot el text viu en UN sol bloc dins "content" (introLbl,
            // més avall), perquè mai més quedi partit entre dues caixes que es puguin xafar.
            var card = MakeCardShell(6, "Pop-Out G1000/G3000", "", out var content);
            content.Location = new Point(content.Location.X, 175);

            var introLbl = new Label
            {
                Text = "A free community tool called POPM — SFS doesn't provide technical support for it.\n\n" +
                       "Alt+G can pop out screens, but always keeps the Windows title bar. POPM removes it — and for G3000, it's the only way to make TSC1/TSC2 touch-enabled.\n\n" +
                       "Different builds for MSFS 2020 and 2024. Download, install, then open our quick setup guide below.",
                Font = new Font("Segoe UI", 19f),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(0, 0),
                AutoSize = true,
                MaximumSize = new Size(456, 0) // amplada fixa, alçada la que calgui segons el text
            };
            content.Controls.Add(introLbl);

            int btnY = introLbl.Bottom + 24;

            var download2020Btn = new Button
            {
                Text = "Download for MSFS 2020",
                Size = new Size(456, 70),
                Location = new Point(0, btnY),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold)
            };
            download2020Btn.FlatAppearance.BorderSize = 0;
            download2020Btn.Click += (s, e) => OpenUrlOnPrimaryMonitor("https://github.com/hawkeye-stan/msfs-popout-panel-manager/releases");
            content.Controls.Add(download2020Btn);
            btnY += 80;

            var download2024Btn = new Button
            {
                Text = "Download for MSFS 2024",
                Size = new Size(456, 70),
                Location = new Point(0, btnY),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold)
            };
            download2024Btn.FlatAppearance.BorderSize = 0;
            download2024Btn.Click += (s, e) => OpenUrlOnPrimaryMonitor("https://github.com/hawkeye-stan/msfs-2024-popout-panel-manager/releases");
            content.Controls.Add(download2024Btn);
            btnY += 80;

            var setupGuideBtn = new Button
            {
                Text = "Setup Guide →",
                Size = new Size(456, 70),
                Location = new Point(0, btnY),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 90, 130),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold)
            };
            setupGuideBtn.FlatAppearance.BorderSize = 0;
            setupGuideBtn.Click += (s, e) => ShowPopoutGuide();
            content.Controls.Add(setupGuideBtn);

            // "content" és un Panel normal: retalla (clip) qualsevol fill que caigui fora de la
            // seva pròpia mida. Com que aquí el text és llarg i "content" es va pujar a Y=175,
            // l'alçada per defecte de MakeCardShell (590) es quedava curta i tallava els botons
            // de sota — per això només es veia mig botó. Cal ajustar-la al contingut real.
            content.Size = new Size(456, setupGuideBtn.Bottom + 8);
            card.Height = content.Bottom + 40;

            return card;
        }

        /// <summary>Torna a llegir l'estat real del registre (font de veritat) i actualitza el toggle sense disparar CheckedChanged.</summary>
        private void RefreshEdgeSwipeState()
        {
            if (_edgeSwipeToggle == null || _edgeSwipeStatusLbl == null) return;

            bool? state = EdgeSwipeConfig.IsDisabled();

            _edgeSwipeUpdating = true;
            _edgeSwipeToggle.Checked = state == true;
            _edgeSwipeUpdating = false;

            if (state == true)
            {
                _edgeSwipeStatusLbl.ForeColor = Color.FromArgb(21, 115, 71);
                _edgeSwipeStatusLbl.Text = "Disabled — already applied.";
            }
            else if (state == false)
            {
                _edgeSwipeStatusLbl.ForeColor = Color.FromArgb(102, 114, 127);
                _edgeSwipeStatusLbl.Text = "Enabled (default Windows behavior).";
            }
            else
            {
                _edgeSwipeStatusLbl.ForeColor = Color.FromArgb(102, 114, 127);
                _edgeSwipeStatusLbl.Text = "Not configured yet.";
            }
        }
    }
}
