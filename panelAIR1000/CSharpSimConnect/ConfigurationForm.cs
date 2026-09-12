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
            var screen4k = GetPanelAir4kScreen();

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

            const int iconSize = 44;

            // Helpers locals: una línia normal a tota l'amplada, o una línia curta amb una icona
            // a la seva dreta. La icona sempre queda ben enganxada al text perquè la línia és
            // prou curta per no fer wrap — mai més pinnejar-la al capdamunt d'un paràgraf llarg
            // (això és el que Pere ha dit que no quedava ben situat).
            Label AddLine(ref int y, string text, int gapBefore = 24)
            {
                y += gapBefore;
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 24f),
                    ForeColor = Color.FromArgb(22, 32, 42),
                    Location = new Point(32, y),
                    AutoSize = true,
                    MaximumSize = new Size(textWidth, 0)
                };
                scrollPanel.Controls.Add(lbl);
                y = lbl.Bottom;
                return lbl;
            }

            Label AddIconLine(ref int y, string text, string resourceName, int gapBefore = 24)
            {
                y += gapBefore;
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 24f),
                    ForeColor = Color.FromArgb(22, 32, 42),
                    Location = new Point(32, y),
                    AutoSize = true,
                    MaximumSize = new Size(textWidth - iconSize - 16, 0)
                };
                scrollPanel.Controls.Add(lbl);
                var icon = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Location = new Point(lbl.Right + 16, lbl.Top),
                    Size = new Size(iconSize, iconSize)
                };
                try
                {
                    using var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                    if (s != null) icon.Image = Image.FromStream(s);
                }
                catch { }
                scrollPanel.Controls.Add(icon);
                y = Math.Max(lbl.Bottom, icon.Bottom);
                return lbl;
            }

            Label AddWarningLine(ref int y, string text, int gapBefore = 8)
            {
                y += gapBefore;
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(153, 27, 27),
                    Location = new Point(32, y),
                    AutoSize = true,
                    MaximumSize = new Size(textWidth, 0)
                };
                scrollPanel.Controls.Add(lbl);
                y = lbl.Bottom;
                return lbl;
            }

            // Ordre real del flux de POPM (corregit per Pere, 2026-09-11, confirmat contra el
            // GETTING_STARTED.md oficial de hawkeye-stan): primer es creen i s'identifiquen TOTS
            // els panells, després es prem la icona verda UN SOL COP (confirma tots els panells
            // alhora, no un per un), després "Start Pop Out", i només un cop les finestres han
            // sortit es poden enganxar les coordenades i activar Hide Title Bar / touch.
            int y = 8;
            AddLine(ref y, "Set up each aircraft you want to fly like this:", 24);
            AddLine(ref y, "1.  Have MSFS already running with your aircraft loaded on the runway, ready to fly — the new profile automatically links to whichever aircraft is currently active.");
            AddIconLine(ref y, "2.  New profile — press the icon, name the aircraft, then Accept.", "panelAIR1000.Resources.popm_new_profile_icon.png");
            AddLine(ref y, "3.  Add panels — one for PFD, one for MFD (plus TSC1 and TSC2 for the TBM 930).");
            AddLine(ref y, "4.  For each panel, press \"Identify\" and click that instrument's screen inside MSFS — a colored circle appears. Repeat for every screen — 2 times for a regular G1000 aircraft (PFD, MFD), 4 times for the TBM 930 (PFD, MFD, TSC1, TSC2).");
            AddIconLine(ref y, "5.  Once every panel is identified, press the green icon once — that confirms them all together and lets you start the pop out.", "panelAIR1000.Resources.popm_confirm_icon.png");
            AddLine(ref y, "6.  Press \"Start Pop Out\" at the bottom.");
            AddWarningLine(ref y, "Don't touch your mouse while this happens — wait until it finishes and the POPM window reappears (it disappears during the process).");
            AddLine(ref y, "7.  The screens will open at an arbitrary position and size. Paste the Top/Left/Width/Height values we provide below under the \"G1000 coordinates\" or \"G3000 coordinates\" title — for G1000, they're the same for any G1000 aircraft. Turn on \"Hide Title Bar\" for every panel.", 16);
            AddIconLine(ref y, "8.  For TSC1 and TSC2 only, also turn on touch screen using the hand icon.", "panelAIR1000.Resources.popm_touch_icon.png");
            AddLine(ref y, "9.  Once the aircraft is fully set up, lock it — turn the padlock green, like in the picture below.");

            int afterStep3Y = y + 32;

            // Captura un 50% més gran que abans (600 -> 900 d'alçada màxima).
            var screenshotBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(32, afterStep3Y),
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

            // G1000 (PFD, MFD) i G3000/TBM 930 (PFD, MFD, TSC1, TSC2): Width/Height ja se saben
            // (fixos, mateixa resolució 3840x2160 sempre), però Top/Left depenen d'on tingui el
            // client la seva pantalla 4K — per això NO es calculen en obrir la guia, només quan es
            // prem el botó corresponent (l'usuari pot moure la pantalla mentre la guia és oberta).
            // Dos jocs de valors de referència diferents perquè MSFS 2020 i MSFS 2024 fan servir
            // builds diferents de POPM amb mides/posicions de panell pròpies (confirmat per Pere,
            // 2026-09-10) — però tots dos capturats amb la pantalla 4K de Pere a la MATEIXA posició
            // real, (0, 1440) (no (0, 1840): correcció de Pere, la pantalla no s'havia mogut).
            const int ref2024Left = 0, ref2024Top = 1440;
            var g1000Panels2024 = new[]
            {
                ("PFD", 1455, 200, 1400, 1040),
                ("MFD", 1455, 2240, 1400, 1040)
            };
            var g3000Panels2024 = new[]
            {
                ("PFD", 1875, 180, 1640, 1025),
                ("MFD", 1875, 2020, 1640, 1025),
                ("TSC1", 3060, 1825, 768, 480),
                ("TSC2", 3060, 2800, 768, 480)
            };

            const int ref2020Left = 0, ref2020Top = 1440;
            var g1000Panels2020 = new[]
            {
                ("PFD", 1480, 238, 1325, 1000),
                ("MFD", 1480, 2278, 1325, 1000)
            };
            var g3000Panels2020 = new[]
            {
                ("PFD", 1898, 193, 1615, 1000),
                ("MFD", 1893, 2035, 1615, 1000),
                ("TSC1", 3040, 1870, 714, 560),
                ("TSC2", 3040, 2825, 714, 560)
            };

            // 2020 a l'esquerra, 2024 a la dreta — aprofita millor l'amplada de la pàgina que
            // apilar-les una sota l'altra.
            const int colGap = 48, colWidth = 700;
            int leftX = 32, rightX = 32 + colWidth + colGap;

            var g1000GroupLbl = new Label
            {
                Text = "G1000 coordinates",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, oneProfileNoteLbl.Bottom + 32),
                AutoSize = true
            };
            scrollPanel.Controls.Add(g1000GroupLbl);
            var g1000_2020 = BuildCoordsSection(scrollPanel, "MSFS 2020", leftX, g1000GroupLbl.Bottom + 16, colWidth, g1000Panels2020, ref2020Left, ref2020Top);
            var g1000_2024 = BuildCoordsSection(scrollPanel, "MSFS 2024", rightX, g1000GroupLbl.Bottom + 16, colWidth, g1000Panels2024, ref2024Left, ref2024Top);
            int g1000Bottom = Math.Max(g1000_2020.Bottom, g1000_2024.Bottom);

            var g3000GroupLbl = new Label
            {
                Text = "G3000 coordinates (TBM 930)",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(32, g1000Bottom + 40),
                AutoSize = true
            };
            scrollPanel.Controls.Add(g3000GroupLbl);
            BuildCoordsSection(scrollPanel, "MSFS 2020", leftX, g3000GroupLbl.Bottom + 16, colWidth, g3000Panels2020, ref2020Left, ref2020Top);
            BuildCoordsSection(scrollPanel, "MSFS 2024", rightX, g3000GroupLbl.Bottom + 16, colWidth, g3000Panels2024, ref2024Left, ref2024Top);

            _popoutGuideForm = guideForm;
            guideForm.Show(this);
        }

        // Construeix un bloc "subtítol + botó Calculate My Top/Left + taula" per a un joc de
        // panells (G1000 o G3000) d'una versió concreta de MSFS, a la columna X indicada (2020 a
        // l'esquerra, 2024 a la dreta). Retorna la taula perquè el bloc de sota pugui col·locar-se
        // relatiu al seu final (mateix patró de "posicionar-se sempre relatiu al .Bottom real" que
        // ja fem servir arreu en aquesta pantalla per no re-endevinar coordenades fixes).
        private DataGridView BuildCoordsSection(Panel parent, string title, int x, int y, int width, (string Name, int RefTop, int RefLeft, int Width, int Height)[] panels, int refScreenLeft, int refScreenTop)
        {
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 19f, FontStyle.Bold),
                ForeColor = Color.FromArgb(102, 114, 127),
                Location = new Point(x, y),
                AutoSize = true
            };
            parent.Controls.Add(titleLbl);

            var calcBtn = new Button
            {
                Text = "Calculate My Top/Left",
                Size = new Size(280, 56),
                Location = new Point(x, titleLbl.Bottom + 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            calcBtn.FlatAppearance.BorderSize = 0;
            parent.Controls.Add(calcBtn);

            var grid = new DataGridView
            {
                Location = new Point(x, calcBtn.Bottom + 16),
                Size = new Size(Math.Min(700, width), 40 + 40 * panels.Length + 4),
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
            grid.Columns.Add("panel", "Panel");
            grid.Columns.Add("top", "Top");
            grid.Columns.Add("left", "Left");
            grid.Columns.Add("width", "Width");
            grid.Columns.Add("height", "Height");
            foreach (DataGridViewColumn col in grid.Columns) col.Width = 130;
            foreach (var p in panels)
                grid.Rows.Add(p.Name, "", "", p.Width, p.Height);
            // És només informativa, no editable: sense això, DataGridView selecciona sol la
            // primera cel·la i la pinta de blau, com si convidés a fer-hi clic.
            grid.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.BackColor;
            grid.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.ForeColor;
            grid.ClearSelection();
            grid.CurrentCell = null;
            parent.Controls.Add(grid);

            // Valors de referència capturats al propi equip de Pere — es desplacen pel delta entre
            // la posició de la seva pantalla 4K en aquell moment (refScreenLeft/Top) i la posició
            // real detectada de la pantalla 4K d'aquest client, calculat al moment de prémer el
            // botó (no en obrir la guia).
            var calculatedColor = Color.FromArgb(235, 91, 37);
            calcBtn.Click += (s, e) =>
            {
                var screen4kNow = GetPanelAir4kScreen();
                int deltaX = screen4kNow.Bounds.Left - refScreenLeft;
                int deltaY = screen4kNow.Bounds.Top - refScreenTop;
                for (int i = 0; i < panels.Length; i++)
                {
                    var topCell = grid.Rows[i].Cells["top"];
                    var leftCell = grid.Rows[i].Cells["left"];
                    topCell.Value = panels[i].RefTop + deltaY;
                    leftCell.Value = panels[i].RefLeft + deltaX;
                    // Taronja com el botó: distingeix d'un cop d'ull els valors calculats dels fixos.
                    topCell.Style.ForeColor = calculatedColor;
                    topCell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                    leftCell.Style.ForeColor = calculatedColor;
                    leftCell.Style.Font = new Font(grid.Font, FontStyle.Bold);
                }
            };

            return grid;
        }

        private static Screen GetPanelAir4kScreen()
        {
            return Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                ?? Screen.AllScreens.FirstOrDefault(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                ?? Screen.PrimaryScreen
                ?? Screen.AllScreens[0];
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
                    statusLbl.Text = "Drag the box for your new panelAIR screen (3840×2160) to match where it really sits next to your main screen. Click “Identify” if you're not sure which one it is.\n\nThen press Apply.";
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
                Text = "POPM is a free tool made by other flight simmers. It lets you pop out your screens automatically and place them exactly within panelAIR's bezels. (SFS doesn't provide support for how this software works.)\n\n" +
                       "You can already pop out screens in MSFS with \"Alt+G\", but that always leaves a title bar on top. POPM removes that bar completely, and for the G3000 it's the only way to make the TSC1 and TSC2 screens respond to touch.\n\n" +
                       "There's a separate version for MSFS 2020 and for MSFS 2024. Download the one for your simulator below, unzip it anywhere you like, run the program inside, then open our step-by-step guide.",
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
