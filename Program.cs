using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SpotifyAutomator
{
    class Program
    {
        const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        const byte KEYEVENTF_KEYUP = 0x02;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static Dictionary<IntPtr, string> spotifyWindowHandles = new Dictionary<IntPtr, string>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            UpdateSpotifyHandles();

            while (true)
            {
                CheckSpotifyTitles();
            }
        }

        static void UpdateSpotifyHandles()
        {
            var spotifyProcesses = System.Diagnostics.Process.GetProcessesByName("Spotify");

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out int processId);

                foreach (var process in spotifyProcesses)
                {
                    if (process.Id == processId)
                    {
                        if (!spotifyWindowHandles.ContainsKey(hWnd))
                        {
                            spotifyWindowHandles[hWnd] = "";
                        }
                        break;
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        static void CheckSpotifyTitles()
        {
            var handlesToRemove = new List<IntPtr>();

            foreach (var hWnd in spotifyWindowHandles.Keys)
            {
                StringBuilder sb = new StringBuilder(512);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (title != spotifyWindowHandles[hWnd])
                {
                    spotifyWindowHandles[hWnd] = title;

                    if (title.Contains("-"))
                    {
                        Console.WriteLine("Now playing: " + title);
                    }

                    if (title.Contains("DJ - Up next"))
                    {
                        Console.WriteLine("Fuck off DJ");
                        keybd_event(VK_MEDIA_NEXT_TRACK, 0, 0, 0);
                        keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, 0);
                    }
                }

                if (string.IsNullOrEmpty(title))
                {
                    handlesToRemove.Add(hWnd);
                }
            }

            foreach (var hWnd in handlesToRemove)
            {
                spotifyWindowHandles.Remove(hWnd);
            }
        }
    }
}
