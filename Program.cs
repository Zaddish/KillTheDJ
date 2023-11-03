using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;


namespace SpotifyAutomator {
    static class Program {
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        private const int WM_APPCOMMAND = 0x0319;
        private static WinEventDelegate dele = null;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        static readonly HttpClient client = new HttpClient();

        private static string[] spinnerAnimationFrames;
        private static Random random = new Random();
        private static string pauseIcon = "⏸︎";
        private static string closeIcon = "⏏︎";

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

        public enum SpotifyState {
            Paused,
            Playing,
            Resumed,
            Closed
        }

        private static SpotifyState currentState;
        public static SpotifyState _currentState {
            get { return currentState; }
            set {
                currentState = value;
                OnStateChanged();
            }
        }
        private static string lastPlayedTitle = "";
        public static string currentlyPlayingTitle = "";
        private static Dictionary<IntPtr, string> spotifyWindowHandles = new Dictionary<IntPtr, string>();
        private static IntPtr m_hhook = IntPtr.Zero;

        const string playingFilePath = "playing.txt";
        private static AutoResetEvent stateChangedEvent = new AutoResetEvent(false);

        public static async Task Main(string[] args) {
            Console.OutputEncoding = Encoding.UTF8;
            DateTime today = DateTime.Today;
            spinnerAnimationFrames = EventSpinner(today);
            if (!File.Exists(playingFilePath)) {
                File.Create(playingFilePath).Close();
            }
            await CheckAndUpdateDJVersionAsync("https://api.github.com/repos/zaddish/killthedj/releases/latest");
            StartDJKiller();
            Console.ReadKey();
        }
       

        private static string[] EventSpinner(DateTime date) {
            // Event spinners based on date
            switch (date.ToString("MM-dd")) {
                case "12-25":
                return new string[] { "🌲", "🎄" };
                case "04-17":
                return new string[] { "🐣", "🐇", "🌷", "🥚", "✝️", "🐥", "🌼", "🍫", "🕊️", "🐰" };
                default:
                string[][] spinners = new string[][] {
                    new string[] { "⠁  ", "⠂  ", "⠄  ", "⡀  ", "⡈  ", "⡐  ", "⡠  ", "⣀  ", "⣁  ", "⣂  ", "⣄  ", "⣌  ", "⣔  ", "⣤  ", "⣥  ", "⣦  ", "⣮  ", "⣶  ", "⣷  ", "⣿  ", "⡿  ", "⠿  ", "⢟  ", "⠟  ", "⡛  ", "⠛  ", "⠫  ", "⢋  ", "⠋  ", "⠍  ", "⡉  ", "⠉  ", "⠑  ", "⠡  ", "⢁  "},
                    new string[] { "⣼  ", "⣹  ", "⢻  ", "⠿  ", "⡟  ", "⣏  ", "⣧  ", "⣶  "},
                    new string[] { "⠁  ", "⠂  ", "⠄  ", "⡀  ", "⢀  ", "⠠  ", "⠐  ", "⠈  "},
                    new string[] { "⢄  ", "⢂  ", "⢁  ", "⡁  ", "⡈  ", "⡐  ", "⡠  "},
                    new string[] { "⢹  ", "⢺  ", "⢼  ", "⣸  ", "⣇  ", "⡧  ", "⡗  ", "⡏  "},
                    new string[] { "⠁  ", "⠁  ", "⠉  ", "⠙  ", "⠚  ", "⠒  ", "⠂  ", "⠂  ", "⠒  ", "⠲  ", "⠴  ", "⠤  ", "⠄  ", "⠄  ", "⠤  ", "⠠  ", "⠠  ", "⠤  ", "⠦  ", "⠖  ", "⠒  ", "⠐  ", "⠐  ", "⠒  ", "⠓  ", "⠋  ", "⠉  ", "⠈  ", "⠈  "},
                    new string[] { "⠁  ", "⠉  ", "⠙  ", "⠚  ", "⠒  ", "⠂  ", "⠂  ", "⠒  ", "⠲  ", "⠴  ", "⠤  ", "⠄  ", "⠄  ", "⠤  ", "⠴  ", "⠲  ", "⠒  ", "⠂  ", "⠂  ", "⠒  ", "⠚  ", "⠙  ", "⠉  ", "⠁  "},
                    new string[] { "⠋  ", "⠙  ", "⠚  ", "⠞  ", "⠖  ", "⠦  ", "⠴  ", "⠲  ", "⠳  ", "⠓  "}
                };
                int index = random.Next(spinners.Length);
                return spinners[index];
            }
        }
        static void StartDJKiller() {
            Process[] spotifyProcesses = Process.GetProcessesByName("Spotify");
            if (spotifyProcesses.Length > 0) {
                uint spotifyProcessId = (uint)spotifyProcesses[0].Id; // If it's not the first process get rekt bozo lol
                var process = Process.GetProcessById((int)spotifyProcessId);
                if (process != null && process.ProcessName.Equals("Spotify", StringComparison.OrdinalIgnoreCase)) {
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, args) => {
                        _currentState = SpotifyState.Closed;
                        while (true) {
                            if (_currentState == SpotifyState.Closed) {
                                Process[] processes;
                                do {
                                    Thread.Sleep(1000);
                                    processes = Process.GetProcessesByName("Spotify");
                                } while (processes.Length == 0);

                                Console.Write("Spotify has reconnected, reinitializing...");
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
                Console.Write("Spotify is not running. Please open spotify before opening the DJ Killer");
                Console.ReadKey();
                AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                    stateChangedEvent.Set();
                    stateChangedEvent.Dispose();
                };
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
                        _currentState = SpotifyState.Playing;
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
                    if (windowTitle.Contains("-") && _currentState == SpotifyState.Paused && windowTitle.Equals(lastPlayedTitle)) {
                        _currentState = SpotifyState.Resumed;
                        return;
                    }

                    // Spotify is now playing a different track
                    if (windowTitle.Contains("-") && !windowTitle.Equals(oldTitle)) {
                        _currentState = SpotifyState.Playing;
                        lastPlayedTitle = windowTitle;
                    } else if (!windowTitle.Contains("-") && _currentState != SpotifyState.Paused) {
                        _currentState = SpotifyState.Paused;
                    }

                    if (windowTitle.Contains("DJ - Up next")) {
                        SkipTrack(hwnd);
                    }
                }
            }
        }
        static string GetWindowTitle(IntPtr hWnd) {
            StringBuilder sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            currentlyPlayingTitle = sb.ToString();
            return sb.ToString();
        }
        static void SkipTrack(IntPtr hWnd) {
            SendMessage(hWnd, WM_APPCOMMAND, hWnd, new IntPtr(APPCOMMAND_MEDIA_NEXTTRACK << 16));
        }


        private static CancellationTokenSource spinnerCancellationTokenSource;
        private static void OnStateChanged() {
            spinnerCancellationTokenSource?.Cancel();
            spinnerCancellationTokenSource = new CancellationTokenSource();

            stateChangedEvent.Set();
            string statusPrefix = currentState switch {
                SpotifyState.Closed => "Spotify was disconnected: ",
                SpotifyState.Paused => "Music Paused: ",
                SpotifyState.Playing => "Now Playing: ",
                SpotifyState.Resumed => "Music Resumed: ",
                _ => ""
            };

            string status = statusPrefix + currentlyPlayingTitle;

            Console.Write("\r" + new string(' ', Console.WindowWidth));
            Console.Write("\r");

            switch (currentState) {
                case SpotifyState.Playing:
                case SpotifyState.Resumed:
                Task.Run(() => AnimateSpinner(status, spinnerCancellationTokenSource.Token));
                break;
                case SpotifyState.Paused:
                Console.Write($"{statusPrefix} {pauseIcon}");
                break;
                case SpotifyState.Closed:
                Console.Write($"{statusPrefix} {closeIcon}");
                break;
            }

            if (currentlyPlayingTitle != "DJ - Up next") {
                if (currentState != SpotifyState.Paused && currentState != SpotifyState.Closed) {
                    File.WriteAllText(playingFilePath, currentlyPlayingTitle);
                } else {
                    File.WriteAllText(playingFilePath, "");
                }
            }

            AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                spinnerCancellationTokenSource?.Cancel();
                spinnerCancellationTokenSource?.Dispose();
                stateChangedEvent.Set();
                stateChangedEvent.Dispose();
            };
        }
        private static async Task CheckAndUpdateDJVersionAsync(string apiUrl) {
            Console.Clear();
            string userAgentName = "Kill-The-DJ";
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            void PrintDJStatus(string message) =>
                Console.WriteLine($@"
`.`.    `.`.     Kill The DJ
  `.`.    `.`.    - Zaddish
   .`.`    .`.`
 .'.'    .'.'    v{assemblyVersion} {message}
' '     ' '      
");

            try {
                if (client.DefaultRequestHeaders.UserAgent.Count == 0) {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgentName, assemblyVersion));
                }

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseBody)) {
                    JsonElement root = doc.RootElement;
                    string releaseName = root.GetProperty("name").GetString();
                    Version releaseVersion = new Version(releaseName);
                    Version currentVersion = new Version(assemblyVersion);

                    if (releaseVersion > currentVersion) {
                        PrintDJStatus($"- You're out of date! Newest version is v{releaseName}");
                        Console.WriteLine("Update to the latest version here: https://github.com/Zaddish/KillTheDJ/releases/");
                    } else {
                        PrintDJStatus("- latest");
                    }
                }
            } catch (HttpRequestException e) {
                PrintDJStatus($"- Error: {e.Message}");
            } catch (Exception ex) {
                PrintDJStatus($"- Unexpected Error: {ex.Message}");
            }

        }
        private static void AnimateSpinner(string title, CancellationToken token) {
            int animationFrame = 0;
            while (!token.IsCancellationRequested) {
                Console.Write($"\r{title} {spinnerAnimationFrames[animationFrame]}");
                animationFrame = (animationFrame + 1) % spinnerAnimationFrames.Length;
                Thread.Sleep(100);
            }
        }
    }
}