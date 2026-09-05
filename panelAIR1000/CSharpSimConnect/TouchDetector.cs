using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimConnect3
{
    /// <summary>
    /// Confirma que el monitor tàctil està configurat només quan es detecta un toc real
    /// al panell (no quan es clica el botó que obre la configuració tàctil de Windows).
    /// </summary>
    public static class TouchDetector
    {
        public static event Action? TouchDetected;
        private static bool _installed;

        /// <summary>
        /// Cal cridar-ho un cop a l'arrencada (abans de Application.Run). Fa servir un
        /// IMessageFilter en lloc d'un WndProc per-control perquè WM_POINTERDOWN es lliura
        /// directament al control que hi ha sota el dit (p. ex. pbMain), no al Form pare.
        /// </summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            Application.AddMessageFilter(new PointerMessageFilter());
        }

        public static void ReportTouch()
        {
            if (AppSettings.Instance.TouchScreenConfigured) return;
            AppSettings.Instance.TouchScreenConfigured = true;
            AppSettings.Instance.Save();
            TouchDetected?.Invoke();
        }

        private const int WM_POINTERDOWN = 0x0246;

        [DllImport("user32.dll")]
        private static extern bool GetPointerType(uint pointerId, out POINTER_INPUT_TYPE pointerType);

        private enum POINTER_INPUT_TYPE { PT_POINTER = 1, PT_TOUCH = 2, PT_PEN = 3, PT_MOUSE = 4, PT_TOUCHPAD = 5 }

        private class PointerMessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_POINTERDOWN)
                {
                    uint pointerId = (uint)((int)m.WParam & 0xFFFF);
                    if (GetPointerType(pointerId, out var pointerType) && pointerType == POINTER_INPUT_TYPE.PT_TOUCH)
                        ReportTouch();
                }
                return false; // no consumir el missatge: ha de seguir el seu curs normal
            }
        }
    }
}
