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
            // Només la pantalla Xiaomi (XMI3446): és l'única que s'ha venut mai amb panelAIR.
            // El model antic (RTK1B1A) ja no compta com a vàlid.
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%XMI3446%' AND ConfigManagerErrorCode = 0"))
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

            if (!AppSettings.Instance.WelcomeCompleted)
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