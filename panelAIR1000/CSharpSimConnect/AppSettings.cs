using System;
using System.IO;
using System.Text.Json;

namespace SimConnect3
{
    /// <summary>
    /// Preferències persistents de panelAIR, separades de la interfície gràfica.
    /// Es guarden a %APPDATA%\panelAIR\settings.json.
    /// </summary>
    public class AppSettings
    {
        public bool WelcomeCompleted { get; set; } = false;
        public bool FirstConfigShown { get; set; } = false;
        public bool TouchScreenConfigured { get; set; } = false;
        public string SfsMonitorPath { get; set; } = @"C:\Program Files\SFSmonitor\SFSmonitor.exe";
        public bool SfsMonitorAutoLaunch { get; set; } = true;

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "panelAIR");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        private static AppSettings? _instance;
        public static AppSettings Instance => _instance ??= Load();

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                        return loaded;
                }
            }
            catch
            {
                // JSON buit, mal format o il·legible: es cau als valors predeterminats sense bloquejar l'app.
            }

            var defaults = new AppSettings();
            try { defaults.Save(); } catch { }
            return defaults;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // No bloquejar l'app si el disc no és escrivible.
            }
        }

        public void ResetToDefaults()
        {
            WelcomeCompleted = false;
            FirstConfigShown = false;
            TouchScreenConfigured = false;
            SfsMonitorPath = @"C:\Program Files\SFSmonitor\SFSmonitor.exe";
            SfsMonitorAutoLaunch = true;
            Save();
        }
    }
}
