#define WINDOWS

using System;
using System.Runtime.InteropServices;

namespace anime_downloader.Classes
{
    public static class OperatingSystemApi
    {
        private const int Restore = 9;

        /// <summary>
        ///     Focus the opened downloader.
        /// </summary>
        public static void FocusDownloader()
        {
            var hwnd = FindWindow(null, "Anime Downloader");
            ShowWindow(hwnd, Restore);
            SetForegroundWindow(hwnd);
        }

#if WINDOWS
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string sClassName, string sAppName);
#endif

#if LINUX
        private static bool SetForegroundWindow(IntPtr hwnd) => false;

        private static bool ShowWindow(IntPtr hWnd, int nCmdShow) => false;

        private static IntPtr FindWindow(string sClassName, string sAppName) => IntPtr.Zero;
#endif
    }
}