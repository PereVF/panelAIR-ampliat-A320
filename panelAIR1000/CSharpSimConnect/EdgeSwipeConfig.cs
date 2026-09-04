using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace SimConnect3
{
    /// <summary>
    /// Activa/desactiva el gest tàctil de Windows "lliscar des de la vora de la pantalla"
    /// (el que obre el Centre de notificacions / calendari).
    ///
    /// Fa servir la mateixa clau que documenta Microsoft per a dispositius tipus panell/quiosc
    /// (Windows IoT Enterprise, "edge swipe policy"):
    ///   HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\EdgeUI
    ///       AllowEdgeSwipe (DWORD) = 0  -> desactivat
    ///       AllowEdgeSwipe (DWORD) = 1  -> activat
    ///
    /// Com que és una clau a HKEY_LOCAL_MACHINE, cal escriure-hi amb permisos d'administrador.
    /// panelAIR normalment NO s'executa com a administrador, així que en lloc d'elevar tota
    /// l'aplicació, aquesta classe llança només "reg.exe" en mode elevat (Verb = "runas").
    /// Windows mostrarà UNA sola finestra de confirmació d'UAC (tret que l'equip ja tingui
    /// l'UAC desactivat, com sol passar en imatges de panell/quiosc tancades).
    /// </summary>
    public static class EdgeSwipeConfig
    {
        private const string PolicyKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\EdgeUI";
        private const string ValueName = "AllowEdgeSwipe";

        /// <summary>
        /// Consulta l'estat actual sense necessitat de permisos d'administrador
        /// (llegir HKLM és sempre permès; només escriure-hi requereix elevació).
        /// Retorna null si la política encara no s'ha tocat mai en aquest equip (valor per defecte de Windows).
        /// </summary>
        public static bool? IsDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(PolicyKeyPath);
                var raw = key?.GetValue(ValueName);
                if (raw is int i) return i == 0;
                return null; // clau/valor no existeix encara
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Desactiva el gest de vora a tot l'equip (per a tots els usuaris).</summary>
        /// <returns>true si l'usuari ha acceptat l'UAC i el canvi s'ha aplicat correctament.</returns>
        public static bool Disable() => SetPolicy(disable: true);

        /// <summary>Reactiva el gest de vora (per desfer el canvi, per exemple des del menú de Configuració).</summary>
        public static bool Enable() => SetPolicy(disable: false);

        private static bool SetPolicy(bool disable)
        {
            string value = disable ? "0" : "1";
            string args =
                $"add \"HKLM\\{PolicyKeyPath}\" /v {ValueName} /t REG_DWORD /d {value} /f";

            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = args,
                UseShellExecute = true,   // imprescindible per poder fer servir Verb = "runas"
                Verb = "runas",           // provoca el diàleg d'elevació (UAC)
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 1223 = ERROR_CANCELLED: l'usuari ha clicat "No" al diàleg d'UAC
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reinicia l'Explorer de Windows perquè el canvi s'apliqui a l'instant,
        /// sense necessitat de reiniciar tot l'ordinador. No requereix permisos d'administrador.
        /// </summary>
        public static void ApplyImmediately()
        {
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); } catch { /* ignore */ }
            }
            Process.Start("explorer.exe");
        }
    }
}
