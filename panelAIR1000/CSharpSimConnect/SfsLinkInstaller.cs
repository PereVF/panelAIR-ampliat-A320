using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>
    /// Descarrega SFS Link i l'instal·la a totes les carpetes Community de MSFS detectades al PC,
    /// demanant sempre el consentiment explícit de l'usuari abans d'escriure a cadascuna.
    /// </summary>
    public static class SfsLinkInstaller
    {
        private const string DownloadUrl = "https://simflightservices.com/wp-content/uploads/sfs-downloads/SFS-link.zip";
        private static readonly HttpClient _http = new HttpClient();

        public static async Task InstallAsync(IWin32Window owner)
        {
            var installs = MsfsInstallDetector.DetectAll();
            if (installs.Count == 0)
            {
                MessageBox.Show(owner,
                    "No Microsoft Flight Simulator installation was detected on this PC.",
                    "SFS Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var consented = new List<MsfsInstall>();
            foreach (var install in installs)
            {
                var result = MessageBox.Show(owner,
                    $"Install SFS Link into this Community folder?\n\n{install.Label}\n{install.CommunityPath}",
                    "SFS Link — installation consent",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
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
                MessageBox.Show(owner,
                    $"Could not download or extract SFS Link:\n{ex.Message}",
                    "SFS Link", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            MessageBox.Show(owner, summary, "SFS Link", MessageBoxButtons.OK,
                failedInto.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
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
