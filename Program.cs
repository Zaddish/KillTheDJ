using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyAutomator {
    static class Program {
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        private const int WM_APPCOMMAND = 0x0319;
        private static WinEventDelegate dele = null;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);


        private enum SpotifyState {
            Paused,
            Playing,
            Resumed,
            Closed
        }

        private static SpotifyState currentState = SpotifyState.Paused;
        private static string lastPlayedTitle = "";
        private static Dictionary<IntPtr, string> spotifyWindowHandles = new Dictionary<IntPtr, string>();
        private static IntPtr m_hhook = IntPtr.Zero;

        static void Main(string[] args) {
            
            Process[] spotifyProcesses = Process.GetProcessesByName("Spotify");
            if (spotifyProcesses.Length > 0) {
                uint spotifyProcessId = (uint)spotifyProcesses[0].Id; // If it's not the first process get rekt bozo lol
                var process = Process.GetProcessById((int)spotifyProcessId);
                if (process != null && process.ProcessName.Equals("Spotify", StringComparison.OrdinalIgnoreCase)) {
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, args) => {
                        currentState = SpotifyState.Closed;
                        while (true) {
                            if (currentState == SpotifyState.Closed) {
                                Console.WriteLine("Spotify has been closed, waiting for it to restart...");
                                Process[] processes;
                                do {
                                    Thread.Sleep(1000);
                                    processes = Process.GetProcessesByName("Spotify");
                                } while (processes.Length == 0);

                                Console.WriteLine("Spotify has restarted, reinitializing...");
                                Main(new string[] { });
                            }
                        }
                    };
                }
                dele = new WinEventDelegate(WinEventProc);
                IntPtr m_hhook = SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, dele, spotifyProcessId, 0, WINEVENT_OUTOFCONTEXT);
                InitializeSpotifyState(spotifyProcessId);
                Application.Run();
            } else {
                Console.WriteLine("Spotify is not running. Please open spotify before opening the DJ Killer");
                Console.ReadKey();
                Application.Exit();
            }
        }

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        static void InitializeSpotifyState(uint spotifyProcessId) {
            EnumWindows((hWnd, lParam) => {
                GetWindowThreadProcessId(hWnd, out int processId);
                if ((uint)processId == spotifyProcessId) {
                    string windowTitle = GetWindowTitle(hWnd);
                    if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Contains("-")) {
                        spotifyWindowHandles[hWnd] = windowTitle;
                        lastPlayedTitle = windowTitle;
                        currentState = SpotifyState.Playing;
                        Console.WriteLine("Now Playing: " + windowTitle);
                    } else {
                        spotifyWindowHandles[hWnd] = windowTitle;
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetWindowThreadProcessId(hwnd, out int processId);
            var process = Process.GetProcessById(processId);
            if (process != null && process.ProcessName.Equals("Spotify", StringComparison.OrdinalIgnoreCase)) {
                string windowTitle = GetWindowTitle(hwnd);
                if (!string.IsNullOrEmpty(windowTitle) && (windowTitle.Contains("Spotify") || windowTitle.Contains("-"))) {
                    string oldTitle = spotifyWindowHandles.ContainsKey(hwnd) ? spotifyWindowHandles[hwnd] : "";
                    spotifyWindowHandles[hwnd] = windowTitle;

                    // Spotify resumed the same track
                    if (windowTitle.Contains("-") && currentState == SpotifyState.Paused && windowTitle.Equals(lastPlayedTitle)) {
                        currentState = SpotifyState.Resumed;
                        Console.WriteLine("Music resumed: " + windowTitle);
                        return;
                    }

                    // Spotify is now playing a different track
                    if (windowTitle.Contains("-") && !windowTitle.Equals(oldTitle)) {
                        currentState = SpotifyState.Playing;
                        lastPlayedTitle = windowTitle;
                        Console.WriteLine("Now Playing: " + windowTitle);
                    } else if (!windowTitle.Contains("-") && currentState != SpotifyState.Paused) {
                        currentState = SpotifyState.Paused;
                        Console.WriteLine("Music paused.");
                    }

                    if (windowTitle.Contains("DJ - Up next")) {
                        Console.WriteLine("Fuck off DJ");
                        SkipTrack(hwnd);
                    }
                }
            }
        }
        static string GetWindowTitle(IntPtr hWnd) {
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        static void SkipTrack(IntPtr hWnd) {
            SendMessage(hWnd, WM_APPCOMMAND, hWnd, new IntPtr(APPCOMMAND_MEDIA_NEXTTRACK << 16));
        }
    }
}
