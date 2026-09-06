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

            bool hasPanel = false;
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_0457%PID_0819%' AND ConfigManagerErrorCode = 0 AND Present = TRUE"))
            {
                hasPanel = searcher.Get().Count > 0;
            }
            bool hasMonitor = false;
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE (DeviceID LIKE '%RTK1B1A%' OR DeviceID LIKE '%XMI3446%') AND ConfigManagerErrorCode = 0"))
            {
                hasMonitor = searcher.Get().Count > 0;
            }

            // Si no té ni el nostre monitor ni el tàctil, sabem del cert que no té panelAIR —
            // mode demo directe (finestra a escala), sense preguntar res més. També mode demo si,
            // per qualsevol motiu, no hi ha cap pantalla 4K real (evita petar el codi que dona per
            // fet que n'hi ha una).
            frmMain.IsDemoMode = (!hasPanel && !hasMonitor) || !has4K;

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