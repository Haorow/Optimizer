using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace Optimizer.Helpers
{
    /// <summary>
    /// Helper pour interagir avec les fenêtres Windows via P/Invoke.
    /// </summary>
    public static class WindowHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        /// <summary>
        /// Récupère toutes les fenêtres visibles avec leur handle et titre.
        /// </summary>
        /// <returns>Liste des fenêtres (Handle, Titre).</returns>
        public static List<(IntPtr Handle, string Title)> GetWindows()
        {
            var windows = new List<(IntPtr Handle, string Title)>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    var title = new StringBuilder(256);
                    GetWindowText(hWnd, title, title.Capacity);
                    if (!string.IsNullOrEmpty(title.ToString()))
                    {
                        windows.Add((hWnd, title.ToString()));
                    }
                }
                return true;
            }, IntPtr.Zero);
            return windows;
        }
    }
}