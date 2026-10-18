namespace SimConnect3
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            TouchDetector.Install();

            bool has4K = Screen.AllScreens.Any(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);

            bool hasMonitor = false;
            // La pantalla panelAIR real reporta com a RTK2775 (confirmat 2026-10-18 amb WMI al
            // mateix hardware físic en 3 PC's diferents). XMI3446 ("Mi Monitor") NO és el panell
            // panelAIR — és un monitor normal que Pere té al seu escriptori, no relacionat.
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%RTK2775%' AND ConfigManagerErrorCode = 0"))
            {
                hasMonitor = searcher.Get().Count > 0;
            }

            // Si la pantalla 4K connectada no és la nostra (Xiaomi), ja sabem del cert que no té
            // panelAIR — mode demo directe (finestra a escala), encara que per casualitat es detecti
            // un xip tàctil (VID_0457/PID_0819, no exclusiu nostre). També mode demo si, per qualsevol
            // motiu, no hi ha cap pantalla 4K real (evita petar el codi que dona per fet que n'hi ha una).
            // El cas "monitor sí, tàctil o RotAIR no" (propietari real amb un cable despenjat) NO és
            // demo — es gestiona al Nivell 2 dins frmMain_Load (missatge "verification failed").
            frmMain.IsDemoMode = !hasMonitor || !has4K;

            // En mode DEMO no hem detectat cap panell real: no té sentit donar la benvinguda com si
            // n'hi hagués un. No marquem WelcomeCompleted tampoc, perquè el dia que hi hagi panell
            // real detectat en aquest mateix PC, la benvinguda de debò encara s'ha de poder mostrar.
            if (!frmMain.IsDemoMode && !AppSettings.Instance.WelcomeCompleted)
            {
                using var welcome = new WelcomeForm();
                if (welcome.ShowDialog() != DialogResult.OK)
                    return;

                AppSettings.Instance.WelcomeCompleted = true;
                AppSettings.Instance.Save();
            }

            Application.Run(new frmMain());
        }
    }
}