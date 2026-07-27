using System;
using System.Collections.Generic;
using System.IO;

namespace SimConnect3
{
    public class MsfsInstall
    {
        public string Label { get; }
        public string CommunityPath { get; }

        public MsfsInstall(string label, string communityPath)
        {
            Label = label;
            CommunityPath = communityPath;
        }
    }

    /// <summary>
    /// Detecta totes les instal·lacions de Microsoft Flight Simulator presents al PC
    /// (2020 i 2024, versió Steam i versió Microsoft Store) i en localitza la carpeta Community.
    /// Mateix mecanisme de lectura de UserCfg.opt que CheckTBM930Mod, generalitzat per a totes les variants.
    /// </summary>
    public static class MsfsInstallDetector
    {
        private static readonly (string Label, string CfgPath)[] Candidates = new[]
        {
            ("MSFS 2020 (Steam)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft Flight Simulator", "UserCfg.opt")),
            ("MSFS 2020 (Microsoft Store)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt")),
            ("MSFS 2024 (Steam)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft Flight Simulator 2024", "UserCfg.opt")),
            ("MSFS 2024 (Microsoft Store)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\UserCfg.opt")),
        };

        public static List<MsfsInstall> DetectAll()
        {
            var results = new List<MsfsInstall>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (label, cfgPath) in Candidates)
            {
                string? communityPath = TryReadCommunityPath(cfgPath);
                if (communityPath == null || !Directory.Exists(communityPath)) continue;
                if (!seenPaths.Add(communityPath)) continue;
                results.Add(new MsfsInstall(label, communityPath));
            }

            return results;
        }

        private static string? TryReadCommunityPath(string cfgPath)
        {
            if (!File.Exists(cfgPath)) return null;
            try
            {
                foreach (var line in File.ReadAllLines(cfgPath))
                {
                    if (!line.TrimStart().StartsWith("InstalledPackagesPath")) continue;
                    int q1 = line.IndexOf('"');
                    int q2 = line.IndexOf('"', q1 + 1);
                    if (q1 >= 0 && q2 > q1)
                        return Path.Combine(line.Substring(q1 + 1, q2 - q1 - 1), "Community");
                }
            }
            catch { }
            return null;
        }
    }
}
