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
            if (!has4K)
            {
                MessageBox.Show(
                    "No compatible 4K display detected.\n\npanelAIR1000 requires a 3840 × 2160 (4K UHD) display to operate.\n\nPlease connect a compatible 4K monitor and restart the application.",
                    "panelAIR1000",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

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