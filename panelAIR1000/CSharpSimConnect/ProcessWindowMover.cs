using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>
    /// Quan panelAIR llança un procés extern (com SFSmonitor) des de la pantalla 4K,
    /// la nova finestra tendeix a obrir-se allà en lloc de la pantalla principal de Windows.
    /// Aquesta classe espera que la finestra principal aparegui i la mou a la pantalla primària.
    /// </summary>
    public static class ProcessWindowMover
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int MaxAttempts = 30; // ~9s a 300ms

        public static void MoveToPrimaryScreenWhenReady(Process process)
        {
            int attempts = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) =>
            {
                attempts++;
                bool done = false;
                try
                {
                    process.Refresh();
                    IntPtr handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        var primary = Screen.PrimaryScreen!.WorkingArea;
                        SetWindowPos(handle, IntPtr.Zero, primary.X + 60, primary.Y + 60, 0, 0,
                            SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                        done = true;
                    }
                }
                catch { done = true; }

                if (done || attempts >= MaxAttempts)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }
    }
}
