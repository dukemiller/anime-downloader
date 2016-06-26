#define WINDOWS

using System;
using System.Runtime.InteropServices;

namespace anime_downloader.Classes
{
    public static class OperatingSystemApi
    {

#if WINDOWS
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string sClassName, string sAppName);
#endif

#if LINUX
        public static bool SetForegroundWindow(IntPtr hwnd)
        {
            return false;
        }

        public static bool ShowWindow(IntPtr hWnd, int nCmdShow)
        {
            return false;
        }

        public static IntPtr FindWindow(string sClassName, string sAppName)
        {
            return IntPtr.Zero;
        }
#endif
    }
}