using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopPet
{
    public static class WindowsApiHelper
    {
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public static TimeSpan GetIdleTime()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (GetLastInputInfo(ref info))
                return TimeSpan.FromMilliseconds(Environment.TickCount - info.dwTime);
            return TimeSpan.Zero;
        }

        public static string GetActiveWindowTitle()
        {
            var hwnd = GetForegroundWindow();
            var buff = new StringBuilder(256);
            if (GetWindowText(hwnd, buff, buff.Capacity) > 0)
                return buff.ToString().ToLower();
            return "";
        }
    }
}
