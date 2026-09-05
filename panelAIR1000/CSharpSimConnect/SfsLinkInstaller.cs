using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimConnect3
{
    /// <summary>
    /// Descarrega SFS Link i l'instal·la a totes les carpetes Community de MSFS detectades al PC,
    /// demanant sempre el consentiment explícit de l'usuari abans d'escriure a cadascuna.
    /// Els diàlegs es mostren com a overlay dins la mateixa finestra de Configuration (mai un
    /// MessageBox/finestra nova, que pot quedar amagada darrere de MSFS en Full Screen).
    /// </summary>
    public static class SfsLinkInstaller
    {
        private const string DownloadUrl = "https://simflightservices.com/wp-content/uploads/sfs-downloads/SFS-link.zip";
        private static readonly HttpClient _http = new HttpClient();

        public static async Task InstallAsync(ConfigurationForm owner)
        {
            var installs = MsfsInstallDetector.DetectAll();
            if (installs.Count == 0)
            {
                await owner.ShowInfoDialogAsync("No Microsoft Flight Simulator installation was detected on this PC.");
                return;
            }

            var consented = new List<MsfsInstall>();
            foreach (var install in installs)
            {
                bool yes = await owner.ShowConfirmDialogAsync(
                    $"Install SFS Link into this Community folder?\n\n{install.Label}\n{install.CommunityPath}",
                    "Yes", "No");
                if (yes)
                    consented.Add(install);
            }

            if (consented.Count == 0) return;

            string tempZip = Path.Combine(Path.GetTempPath(), "panelAIR_SFSLink_" + Guid.NewGuid() + ".zip");
            string tempExtractDir = Path.Combine(Path.GetTempPath(), "panelAIR_SFSLink_" + Guid.NewGuid());

            try
            {
                using (var response = await _http.GetAsync(DownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using var fs = File.Create(tempZip);
                    await response.Content.CopyToAsync(fs);
                }

                Directory.CreateDirectory(tempExtractDir);
                ZipFile.ExtractToDirectory(tempZip, tempExtractDir, overwriteFiles: true);
            }
            catch (Exception ex)
            {
                await owner.ShowInfoDialogAsync($"Could not download or extract SFS Link:\n{ex.Message}");
                return;
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            }

            var installedInto = new List<string>();
            var failedInto = new List<string>();
            foreach (var install in consented)
            {
                try
                {
                    // El zip conté el paquet "solt" (modules/, manifest.json, layout.json) sense carpeta
                    // pròpia; MSFS necessita que cada paquet visqui en la seva pròpia subcarpeta dins Community.
                    string packageDir = Path.Combine(install.CommunityPath, "SFS-Link");
                    CopyDirectory(tempExtractDir, packageDir);
                    installedInto.Add(install.Label);
                }
                catch
                {
                    failedInto.Add(install.Label);
                }
            }

            try { Directory.Delete(tempExtractDir, recursive: true); } catch { }

            string summary = installedInto.Count > 0
                ? "SFS Link installed into:\n" + string.Join("\n", installedInto)
                : "SFS Link was not installed into any folder.";
            if (failedInto.Count > 0)
                summary += "\n\nFailed:\n" + string.Join("\n", failedInto);

            await owner.ShowInfoDialogAsync(summary);
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
