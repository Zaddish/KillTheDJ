using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyAutomator {
    class Program {
        const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        const int WM_APPCOMMAND = 0x0319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static HashSet<int> spotifyProcessIds = new HashSet<int>();
        private static Dictionary<IntPtr, string> spotifyWindowHandles = new Dictionary<IntPtr, string>();
        static System.Threading.Timer handleUpdateTimer;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            handleUpdateTimer = new System.Threading.Timer(UpdateSpotifyHandles, null, 0, 1000);

            while (true) {
                CheckSpotifyTitlesAndSkipTrackIfNeeded();
                Thread.Sleep(1);
            }
        }

        static void UpdateSpotifyHandles(object state) {
            spotifyProcessIds = new HashSet<int>(System.Diagnostics.Process.GetProcessesByName("Spotify").Select(p => p.Id));
            EnumWindows((hWnd, lParam) => {
                GetWindowThreadProcessId(hWnd, out int processId);

                if (spotifyProcessIds.Contains(processId)) {
                    string title = CheckSpotifyTitles(hWnd);
                    if (!string.IsNullOrEmpty(title) && (title.Contains("Spotify") || title.Contains("-"))) {
                        spotifyWindowHandles[hWnd] = title;
                    }
                }
                return true;
            }, IntPtr.Zero);

            RemoveStaleHandles();
        }

        static string CheckSpotifyTitles(IntPtr hWnd) {
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        static void CheckSpotifyTitlesAndSkipTrackIfNeeded() {
            foreach (var kvp in spotifyWindowHandles.ToList()) {
                IntPtr hWnd = kvp.Key;
                string title = CheckSpotifyTitles(hWnd);

                if (!string.IsNullOrEmpty(title) && title != kvp.Value) {
                    spotifyWindowHandles[hWnd] = title;

                    if (title.Contains("-")) {
                        Console.WriteLine("Now playing: " + title);
                    }

                    if (title.Contains("DJ - Up next")) {
                        Console.WriteLine("Fuck off DJ");
                        SkipTrack();
                    }
                }
            }
        }

        static void SkipTrack() {
            foreach (var hWnd in spotifyWindowHandles.Keys) {
                SendMessage(hWnd, WM_APPCOMMAND, hWnd, new IntPtr(APPCOMMAND_MEDIA_NEXTTRACK << 16));
                break;
            }
        }

        static void RemoveStaleHandles() {
            var handlesToRemove = spotifyWindowHandles.Keys
                .Where(hWnd => string.IsNullOrEmpty(CheckSpotifyTitles(hWnd)))
                .ToList();

            foreach (var hWnd in handlesToRemove) {
                spotifyWindowHandles.Remove(hWnd);
            }
        }
    }
}