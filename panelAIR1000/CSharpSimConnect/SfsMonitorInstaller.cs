using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimConnect3
{
    /// <summary>Descarrega i instal·la SFSmonitor en una carpeta fixa local, sense necessitar cap altra acció de l'usuari.</summary>
    public static class SfsMonitorInstaller
    {
        private const string DownloadUrl = "https://simflightservices.com/wp-content/uploads/2026/07/SFSmonitor_v04.zip";
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>Descarrega, desempaqueta i retorna la ruta completa de SFSmonitor.exe, o null si no s'ha trobat dins el paquet.</summary>
        public static async Task<string?> InstallAsync()
        {
            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "panelAIR1000", "SFSmonitor");

            string tempZip = Path.Combine(Path.GetTempPath(), "panelAIR_SFSmonitor_" + Guid.NewGuid() + ".zip");

            using (var response = await _http.GetAsync(DownloadUrl))
            {
                response.EnsureSuccessStatusCode();
                using var fs = File.Create(tempZip);
                await response.Content.CopyToAsync(fs);
            }

            try
            {
                if (Directory.Exists(installDir))
                    Directory.Delete(installDir, recursive: true);
                Directory.CreateDirectory(installDir);
                ZipFile.ExtractToDirectory(tempZip, installDir, overwriteFiles: true);
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            }

            // El nom de l'executable dins el zip pot portar sufix de versió (p. ex. SFSmonitor_v04.exe),
            // per això es busca per patró en lloc del nom literal "SFSmonitor.exe".
            var matches = Directory.GetFiles(installDir, "SFSmonitor*.exe", SearchOption.AllDirectories);
            return matches.Length > 0 ? matches[0] : null;
        }
    }
}
