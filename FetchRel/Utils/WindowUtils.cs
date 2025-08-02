using System;
using System.Runtime.InteropServices;

namespace Utils
{
    public static class WindowUtils
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public static void TrySetWideConsole(int targetWidth = 160)
        {
            try
            {
                if (OperatingSystem.IsWindows() && Console.LargestWindowWidth >= targetWidth)
                {
                    Console.SetWindowSize(targetWidth, Console.WindowHeight);
                    Console.SetBufferSize(targetWidth, Console.BufferHeight);
                }
            }
            catch {}
        }

        public static void CenterConsole()
        {
            try
            {
                if (!OperatingSystem.IsWindows()) return;

                IntPtr hwnd = GetConsoleWindow();
                if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out RECT r)) return;

                int width = r.Right - r.Left;
                int height = r.Bottom - r.Top;
                int left = (GetSystemMetrics(0) - width) / 2;
                int top = (GetSystemMetrics(1) - height) / 2;

                MoveWindow(hwnd, left, top, width, height, true);
            }
            catch {}
        }
    }
}
