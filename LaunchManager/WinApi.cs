using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LaunchManager
{
    internal static class WinApi
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;

        public static void MinimizeMainWindow(Process p)
        {
            try
            {
                p.WaitForInputIdle(10000);
                if (p.MainWindowHandle != IntPtr.Zero)
                    ShowWindow(p.MainWindowHandle, SW_MINIMIZE);
            }
            catch { }
        }
    }
}
