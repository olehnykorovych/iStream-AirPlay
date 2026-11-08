// iStream — AirPlay screen mirroring receiver for Windows (iPhone / iPad → PC).
// Engine: UxPlay (engine\uxplay-windows.exe). iStream configures it for minimum latency
// (decode + rotate + display entirely on the GPU), controls fullscreen and rotation,
// and never restarts the server while a stream is running.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("iStream")]
[assembly: System.Reflection.AssemblyProduct("iStream")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]

namespace IStream
{
    static class Program
    {
        public static string LangOverride;
        public static int ThemeOverride = -1;

        [STAThread]
        static void Main(string[] args)
        {
            string shotDir = null;
            bool minimized = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length) shotDir = args[i + 1];
                if (args[i] == "--lang" && i + 1 < args.Length) LangOverride = args[i + 1];
                if (args[i] == "--theme" && i + 1 < args.Length) ThemeOverride = args[i + 1] == "light" ? 1 : 2;
                if (args[i] == "--minimized") minimized = true;
            }
            try { Native.SetPreferredAppMode(1); } catch { }   // allow dark scrollbars / menus

            bool created;
            using (var mutex = new Mutex(true, @"Local\iStream.SingleInstance", out created))
            {
                if (!created && shotDir == null)
                {
                    IntPtr h = Native.FindWindow(null, "iStream");
                    if (h != IntPtr.Zero) { Native.ShowWindow(h, 9); Native.SetForegroundWindow(h); }
                    return;
                }
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Gfx.Scale = g.DpiX / 96f;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(shotDir, minimized));
            }
        }
    }

    // ------------------------------------------------------------------ settings

    sealed class Settings
    {
        public string DeviceName = "iStream";
        public int Quality = 1;          // 0=720p 1=1080p 2=1440p 3=4K H.265
        public int Fps = 60;
        public int Renderer = 0;         // 0=D3D11 1=D3D12
        public bool LowLatency = true;
        public bool Audio = true;
        public bool AutoFullscreen = true;
        public int Monitor = -1;         // -1 = monitor the window is on
        public int Rotation = 0;         // 0=0° 1=90° right 2=180° 3=90° left
        public bool AutoStart = true;
        public string Language = "en";
        public int ThemeMode = 0;        // 0 = like Windows, 1 = light, 2 = dark

        static string AppData { get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); } }
        public static string Folder { get { return Path.Combine(AppData, "iStream"); } }
        static string FilePath { get { return Path.Combine(Folder, "settings.ini"); } }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                string old = Path.Combine(Path.Combine(AppData, "AirStream"), "settings.ini");   // migracja z AirStream
                if (!File.Exists(FilePath) && File.Exists(old))
                {
                    Directory.CreateDirectory(Folder);
                    File.Copy(old, FilePath);
                }
                if (!File.Exists(FilePath)) return s;
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int i = raw.IndexOf('=');
                    if (i <= 0) continue;
                    string k = raw.Substring(0, i).Trim(), v = raw.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "name": if (v.Length > 0) s.DeviceName = Engine.SafeName(v); break;
                        case "quality": s.Quality = Clamp(ParseInt(v, 1), 0, 3); break;
                        case "fps": s.Fps = ParseInt(v, 60) == 30 ? 30 : 60; break;
                        case "renderer": s.Renderer = Clamp(ParseInt(v, 0), 0, 1); break;
                        case "lowlatency": s.LowLatency = v == "1"; break;
                        case "audio": s.Audio = v == "1"; break;
                        case "autofullscreen": s.AutoFullscreen = v == "1"; break;
                        case "monitor": s.Monitor = ParseInt(v, -1); break;
                        case "rotation": s.Rotation = Clamp(ParseInt(v, 0), 0, 3); break;
                        case "autostart": s.AutoStart = v == "1"; break;
                        case "lang": if (Array.IndexOf(L.Codes, v) >= 0) s.Language = v; break;
                        case "theme": s.ThemeMode = Clamp(ParseInt(v, 0), 0, 2); break;
                    }
                }
                if (s.DeviceName == "AirStream") s.DeviceName = "iStream";
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var sb = new StringBuilder();
                sb.AppendLine("name=" + DeviceName);
                sb.AppendLine("quality=" + Quality.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("fps=" + Fps.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("renderer=" + Renderer.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("lowlatency=" + (LowLatency ? "1" : "0"));
                sb.AppendLine("audio=" + (Audio ? "1" : "0"));
                sb.AppendLine("autofullscreen=" + (AutoFullscreen ? "1" : "0"));
                sb.AppendLine("monitor=" + Monitor.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("rotation=" + Rotation.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("autostart=" + (AutoStart ? "1" : "0"));
                sb.AppendLine("lang=" + Language);
                sb.AppendLine("theme=" + ThemeMode.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        public Settings Clone() { return (Settings)MemberwiseClone(); }

        static int ParseInt(string v, int d)
        {
            int r;
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out r) ? r : d;
        }

        static int Clamp(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
    }

    // ------------------------------------------------------------------ UxPlay engine

    sealed class Engine
    {
        public readonly string Folder;
        public readonly string ExePath;

        public static readonly string[] Resolutions = { "1280x720", "1920x1080", "2560x1440", "3840x2160" };
        public static readonly string[] ShortQuality = { "720p", "1080p", "1440p", "4K H.265" };
        static readonly string[] Directions = { "identity", "90r", "180", "90l" };

        public Engine()
        {
            Folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine");
            ExePath = Path.Combine(Folder, "uxplay-windows.exe");
        }

        public bool Installed { get { return File.Exists(ExePath); } }

        public static string UserArgumentsPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "leapbtw", "uxplay-windows", "arguments.txt");
            }
        }

        public static string MachineArgumentsPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                    "uxplay-windows", "arguments.txt");
            }
        }

        public static string RcPath { get { return Path.Combine(Settings.Folder, "uxplayrc.txt"); } }

        public static string SafeName(string n)
        {
            var sb = new StringBuilder();
            foreach (char c in (n ?? "").Trim())
                if (!char.IsControl(c) && c != '"' && c != '\'' && c != '%' && c != '\\') sb.Append(c);
            string r = sb.ToString().Trim();
            if (r.Length > 40) r = r.Substring(0, 40).Trim();
            return r.Length == 0 ? "iStream" : r;
        }

        // UxPlay options in UXPLAYRC format (one per line, no leading "-").
        // Used instead of arguments.txt because the uxplay-windows wrapper breaks quoted values.
        //  fps 60            UxPlay asks the phone for only 30 FPS by default
        //  vd d3d11h264dec   hardware decoding on the GPU
        //  vc "...convert"   rotation + colour conversion on the GPU (frames never go back to RAM)
        //  vsync no          frames shown immediately, not held for the audio clock
        //  nohold            a new connection takes over at once (fast reconnects)
        public static string BuildRc(Settings s)
        {
            string gpu = s.Renderer == 1 ? "d3d12" : "d3d11";
            var l = new List<string>();
            l.Add("# Generated by iStream - change settings in the app");
            l.Add("n \"" + SafeName(s.DeviceName) + "\"");
            l.Add("nh");
            if (s.Quality == 3) l.Add("h265");
            l.Add(string.Format(CultureInfo.InvariantCulture, "s {0}@{1}", Resolutions[s.Quality], s.Fps));
            l.Add(string.Format(CultureInfo.InvariantCulture, "fps {0}", s.Fps));
            if (s.LowLatency) l.Add("vsync no");
            l.Add("nohold");
            l.Add("vd " + gpu + "h264dec");
            l.Add(string.Format("vc \"{0}upload ! {0}convert video-direction={1}\"", gpu, Directions[s.Rotation]));
            l.Add("vs " + gpu + "videosink");
            if (!s.Audio) l.Add("as 0");
            return string.Join("\n", l) + "\n";   // LF only: UxPlay's parser chokes on CR
        }

        public void WriteConfig(Settings s)
        {
            Directory.CreateDirectory(Settings.Folder);
            File.WriteAllText(RcPath, BuildRc(s), new UTF8Encoding(false));
            Directory.CreateDirectory(Path.GetDirectoryName(UserArgumentsPath));
            File.WriteAllText(UserArgumentsPath, "-nh", new UTF8Encoding(false));
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\leapbtw\uxplay-windows"))
            {
                k.SetValue("renderer_mode", s.Renderer == 1 ? "d3d12" : "d3d11");
                k.SetValue("force_fs_enabled", "false");   // iStream handles fullscreen
                if (k.GetValue("ble_enabled") == null) k.SetValue("ble_enabled", "true");
            }
        }

        public int RunningPid()
        {
            int pid = 0;
            foreach (Process p in Process.GetProcessesByName("uxplay-windows"))
            {
                if (pid == 0) pid = p.Id;
                p.Dispose();
            }
            return pid;
        }

        public void Stop()
        {
            foreach (string name in new[] { "uxplay-windows", "uxplay-bluetooth-beacon" })
                foreach (Process p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                    p.Dispose();
                }
        }

        public void Start(Settings s)
        {
            Stop();
            WriteConfig(s);
            var psi = new ProcessStartInfo(ExePath);
            psi.WorkingDirectory = Folder;
            psi.UseShellExecute = false;
            psi.EnvironmentVariables["UXPLAYRC"] = RcPath;
            Process.Start(psi).Dispose();
        }
    }

    // ------------------------------------------------------------------ firewall

    static class Firewall
    {
        public enum State { Unknown, Ok, Blocked, NoRule }

        public static State Check(string exe)
        {
            try
            {
                Type t = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                if (t == null) return State.Unknown;
                dynamic fw = Activator.CreateInstance(t);
                bool allow = false, block = false;
                foreach (dynamic r in fw.Rules)
                {
                    object appObj = r.ApplicationName;
                    string app = appObj as string;
                    if (string.IsNullOrEmpty(app) || !string.Equals(app, exe, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!(bool)r.Enabled || (int)r.Direction != 1) continue;   // 1 = inbound
                    if ((int)r.Action == 0) block = true; else allow = true;
                }
                if (block) return State.Blocked;
                return allow ? State.Ok : State.NoRule;
            }
            catch { return State.Unknown; }
        }

        // Runs elevated (the user approves the UAC prompt).
        public static bool Fix(string exe)
        {
            string p = exe.Replace("'", "''");
            string script =
                "$p='" + p + "';" +
                "Get-NetFirewallApplicationFilter | Where-Object { $_.Program -eq $p } | Get-NetFirewallRule | Remove-NetFirewallRule;" +
                "Remove-NetFirewallRule -DisplayName 'AirStream - odbiornik AirPlay' -ErrorAction SilentlyContinue;" +
                "Remove-NetFirewallRule -DisplayName 'iStream - AirPlay receiver' -ErrorAction SilentlyContinue;" +
                "New-NetFirewallRule -DisplayName 'iStream - AirPlay receiver' " +
                "-Description 'Lets iPhone/iPad connect to iStream (UxPlay)' " +
                "-Direction Inbound -Program $p -Action Allow -Profile Any | Out-Null";
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                using (Process pr = Process.Start(psi))
                {
                    pr.WaitForExit(60000);
                    return pr.HasExited && pr.ExitCode == 0;
                }
            }
            catch (Win32Exception) { return false; }
        }
    }

    // ------------------------------------------------------------------ video window (fullscreen)

    sealed class VideoWindowManager
    {
        const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        const long WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000, WS_SYSMENU = 0x00080000,
                   WS_MINIMIZEBOX = 0x00020000, WS_MAXIMIZEBOX = 0x00010000, WS_POPUP = 0x80000000,
                   WS_VISIBLE = 0x10000000, WS_OVERLAPPEDWINDOW = 0x00CF0000;
        const long WS_EX_DLGMODALFRAME = 0x1, WS_EX_WINDOWEDGE = 0x100, WS_EX_CLIENTEDGE = 0x200,
                   WS_EX_STATICEDGE = 0x20000;
        const uint SWP_SHOWWINDOW = 0x40, SWP_FRAMECHANGED = 0x20, SWP_NOOWNERZORDER = 0x200;

        public IntPtr Handle { get; private set; }
        bool wantFullscreen, applied, hasSaved;
        IntPtr savedStyle, savedEx;
        Native.RECT savedRect;

        public bool Update(int pid, bool autoFullscreen, Func<IntPtr, Screen> pickScreen)
        {
            IntPtr h = pid == 0 ? IntPtr.Zero : Native.FindVideoWindow(pid);
            if (h != Handle)
            {
                Handle = h;
                hasSaved = false;
                applied = false;
                wantFullscreen = h != IntPtr.Zero && autoFullscreen;
            }
            if (Handle == IntPtr.Zero) return false;

            if (wantFullscreen)
            {
                if (applied && HasCaption(Handle))
                    wantFullscreen = false;          // frame restored by someone else (e.g. Alt+Enter)
                else
                {
                    Screen scr = pickScreen(Handle);
                    if (!applied || !IsCovering(Handle, scr)) Enter(Handle, scr);
                }
            }
            return true;
        }

        public bool IsFullscreen
        {
            get { return Handle != IntPtr.Zero && !HasCaption(Handle) && IsCovering(Handle, Screen.FromHandle(Handle)); }
        }

        public void Toggle(Screen scr)
        {
            if (Handle == IntPtr.Zero) return;
            if (!HasCaption(Handle) && IsCovering(Handle, scr)) { wantFullscreen = false; Exit(Handle, scr); }
            else { wantFullscreen = true; Enter(Handle, scr); }
        }

        static bool HasCaption(IntPtr h)
        {
            return (Native.GetWindowLongPtr(h, GWL_STYLE).ToInt64() & WS_CAPTION) == WS_CAPTION;
        }

        static bool IsCovering(IntPtr h, Screen scr)
        {
            Native.RECT r;
            if (!Native.GetWindowRect(h, out r)) return false;
            Rectangle b = scr.Bounds;
            return Math.Abs(r.Left - b.Left) <= 1 && Math.Abs(r.Top - b.Top) <= 1 &&
                   Math.Abs(r.Right - b.Right) <= 1 && Math.Abs(r.Bottom - b.Bottom) <= 1;
        }

        void Enter(IntPtr h, Screen scr)
        {
            if (!hasSaved)
            {
                savedStyle = Native.GetWindowLongPtr(h, GWL_STYLE);
                savedEx = Native.GetWindowLongPtr(h, GWL_EXSTYLE);
                Native.GetWindowRect(h, out savedRect);
                hasSaved = true;
            }
            if (Native.IsZoomed(h)) Native.ShowWindow(h, 9);

            long style = Native.GetWindowLongPtr(h, GWL_STYLE).ToInt64();
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            style |= WS_POPUP | WS_VISIBLE;
            long ex = Native.GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64();
            ex &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            Native.SetWindowLongPtr(h, GWL_STYLE, new IntPtr(style & 0xFFFFFFFFL));
            Native.SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr(ex & 0xFFFFFFFFL));

            Rectangle b = scr.Bounds;
            Native.SetWindowPos(h, IntPtr.Zero, b.X, b.Y, b.Width, b.Height, SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);
            Native.SetForegroundWindow(h);
            applied = true;
        }

        void Exit(IntPtr h, Screen scr)
        {
            Rectangle wa = scr.WorkingArea;
            var target = new Rectangle(wa.X + wa.Width / 5, wa.Y + wa.Height / 10, wa.Width * 3 / 5, wa.Height * 4 / 5);

            if (hasSaved && (savedStyle.ToInt64() & WS_CAPTION) == WS_CAPTION)
            {
                Native.SetWindowLongPtr(h, GWL_STYLE, savedStyle);
                Native.SetWindowLongPtr(h, GWL_EXSTYLE, savedEx);
                var sr = Rectangle.FromLTRB(savedRect.Left, savedRect.Top, savedRect.Right, savedRect.Bottom);
                if (sr.Width > 200 && sr.Height > 200 && !sr.Contains(scr.Bounds)) target = sr;
            }
            else
            {
                long style = Native.GetWindowLongPtr(h, GWL_STYLE).ToInt64();
                style = (style & ~WS_POPUP) | WS_OVERLAPPEDWINDOW | WS_VISIBLE;
                Native.SetWindowLongPtr(h, GWL_STYLE, new IntPtr(style & 0xFFFFFFFFL));
            }
            Native.SetWindowPos(h, IntPtr.Zero, target.X, target.Y, target.Width, target.Height, SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);
            applied = false;
            hasSaved = false;
        }
    }

    // ------------------------------------------------------------------ WinAPI

    static class Native
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool SetWindowText(IntPtr h, string text);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr h, int idx);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr h, int idx, IntPtr v);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] public static extern int SetWindowTheme(IntPtr h, string app, string idList);

        public static void Dwm(IntPtr h, int attr, int value)
        {
            try { DwmSetWindowAttribute(h, attr, ref value, 4); } catch { }
        }

        public static void RoundCorners(IntPtr h) { Dwm(h, 33, 3); }   // DWMWCP_ROUNDSMALL

        // Undocumented but stable since Windows 10 1903 (used by Explorer, Notepad++ and others).
        [DllImport("uxtheme.dll", EntryPoint = "#135")] public static extern int SetPreferredAppMode(int mode);
        [DllImport("uxtheme.dll", EntryPoint = "#133")] public static extern bool AllowDarkModeForWindow(IntPtr h, bool allow);

        public static void DarkScrollbars(IntPtr h)
        {
            try { AllowDarkModeForWindow(h, true); } catch { }
            try { SetWindowTheme(h, "DarkMode_Explorer", null); } catch { }
        }

        // GStreamer video window (d3d11videosink / d3d12videosink) inside the engine process.
        public static IntPtr FindVideoWindow(int pid)
        {
            IntPtr found = IntPtr.Zero;
            var cls = new StringBuilder(256);
            var title = new StringBuilder(256);
            EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                uint wp;
                GetWindowThreadProcessId(h, out wp);
                if (wp != (uint)pid || !IsWindowVisible(h)) return true;
                cls.Length = 0;
                title.Length = 0;
                GetClassName(h, cls, cls.Capacity);
                GetWindowText(h, title, title.Capacity);
                string c = cls.ToString(), t = title.ToString();
                if (c.StartsWith("Qt", StringComparison.OrdinalIgnoreCase)) return true;
                bool isGst = c.StartsWith("GST", StringComparison.OrdinalIgnoreCase);
                bool byTitle = t.IndexOf("renderer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               t.IndexOf("AirPlay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               t.IndexOf("Direct3D", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isGst || byTitle) { found = h; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }
    }

    // ------------------------------------------------------------------ main window

    sealed class MainForm : Form
    {
        enum Status { Stopped, Busy, Ready, Streaming, Missing }

        sealed class Row
        {
            public string TitleKey, DescKey;
            public Label Title, Desc;
            public Control Ctl;
            public Panel Sep;
        }

        sealed class Section
        {
            public string Key;
            public Label Header;
            public CardPanel Card;
            public readonly List<Row> Rows = new List<Row>();
        }

        sealed class HelpCard
        {
            public string Key;
            public CardPanel Card;
            public Label Title, Body;
        }

        const int HK_FULLSCREEN = 1, HK_ROTATE = 2;
        const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;
        const int SideW = 232, ClientW = 900, ClientH = 720;
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string VideoTitle = "iStream · AirPlay";

        readonly Engine engine = new Engine();
        readonly VideoWindowManager video = new VideoWindowManager();
        readonly string shotDir;
        Settings settings;
        Status status = Status.Stopped;
        string busyKey = "status_starting";
        bool wantRunning, busy, pending, loadingUi, closing, hk1 = true, hk2 = true, fwFixing;
        int lastAutoStart, lastWindowSeen = unchecked(Environment.TickCount - 100000);
        string transientHint;
        int transientUntil;
        Task opTask;
        Firewall.State fwState = Firewall.State.Unknown;
        IntPtr decorated;
        readonly Icon iconSmall, iconBig;
        readonly ToolTip tips = new ToolTip();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int page = -1;

        // sidebar
        Panel side;
        FlatButton navStream, navSettings, navHelp;
        StatusDot sideDot;
        Label sideBrand, sideStatus, sideVersion, sideShortcut;
        PictureBox sideLogo;

        // pages
        Panel content, pageStream, pageSettings, pageHelp;

        // stream page
        StatusDot dot;
        Label lblTitle, lblHint, lblRotation, lblSetup;
        CardPanel fwBanner, pendingBanner, rotCard;
        IconBox fwIcon, pendIcon;
        Label lblFw, lblPendTitle, lblPendBody;
        FlatButton btnFw, btnApplyNow, btnPower, btnFullscreen;
        readonly FlatButton[] rotTiles = new FlatButton[4];
        readonly List<FlatButton> chips = new List<FlatButton>();

        // settings page
        Label setTitle;
        readonly List<Section> sections = new List<Section>();
        SelectBox selQuality, selFps, selRenderer, selMonitor, selLang, selTheme;
        ToggleSwitch tglLowLat, tglAudio, tglAutoFs, tglAutoStart, tglWinStart;
        TextField txtName;

        // help page
        Label helpTitle;
        readonly List<HelpCard> helpCards = new List<HelpCard>();

        static int D(float v) { return Gfx.D(v); }

        public MainForm(string shotDir, bool minimized)
        {
            this.shotDir = shotDir;
            settings = Settings.Load();
            if (Program.LangOverride != null && Array.IndexOf(L.Codes, Program.LangOverride) >= 0)
                settings.Language = Program.LangOverride;   // podgląd, nie jest zapisywany
            L.Lang = settings.Language;

            Text = "iStream";
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.Body;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(D(ClientW), D(ClientH));
            iconBig = MakeIcon(32);
            iconSmall = MakeIcon(16);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon = iconBig; }
            if (minimized) WindowState = FormWindowState.Minimized;

            SetupTooltips();
            Theme.Apply(ResolveDark());
            BuildUi();

            timer.Interval = 300;
            timer.Tick += delegate { Tick(); };
        }

        // ---------------------------------------------------------- theme

        bool ResolveDark()
        {
            int mode = Program.ThemeOverride >= 0 ? Program.ThemeOverride : settings.ThemeMode;
            if (mode == 1) return false;
            if (mode == 2) return true;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object v = k == null ? null : k.GetValue("AppsUseLightTheme");
                    return v is int && (int)v == 0;
                }
            }
            catch { return true; }
        }

        // (Re)creates every control with the current theme colours. Engine state lives in fields and survives.
        void BuildUi()
        {
            SuspendLayout();
            var old = new List<Control>();
            foreach (Control c in Controls) old.Add(c);
            Controls.Clear();
            foreach (Control c in old) c.Dispose();
            sections.Clear();
            helpCards.Clear();
            chips.Clear();

            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
            Controls.Add(content);
            BuildSidebar();
            BuildStreamPage();
            BuildSettingsPage();
            BuildHelpPage();

            int p = page < 0 ? 0 : page;
            page = -1;
            ShowPage(p);
            ResumeLayout();
        }

        void ApplyTheme()
        {
            if (closing) return;
            int scroll = pageSettings != null ? -pageSettings.AutoScrollPosition.Y : 0;
            Theme.Apply(ResolveDark());
            ApplyWindowChrome();
            BuildUi();
            ApplyTexts();
            if (scroll > 0) pageSettings.AutoScrollPosition = new Point(0, scroll);
        }

        void ApplyWindowChrome()
        {
            if (!IsHandleCreated) return;
            Native.Dwm(Handle, 20, Theme.IsDark ? 1 : 0);              // dark / light title bar
            Native.Dwm(Handle, 35, Theme.ColorRef(Theme.Side));        // caption colour (Win11)
            Native.Dwm(Handle, 34, Theme.ColorRef(Theme.Side));        // border colour (Win11)
        }

        void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (closing || settings.ThemeMode != 0 || Program.ThemeOverride >= 0) return;
            if (ResolveDark() == Theme.IsDark) return;
            try { BeginInvoke(new Action(ApplyTheme)); } catch { }
        }

        static Icon MakeIcon(int size)
        {
            using (Bitmap b = IconArt.Draw(size)) return Icon.FromHandle(b.GetHicon());
        }

        // ---------------------------------------------------------- building blocks

        static Label MkLabel(Control parent, Font font, Color color)
        {
            var l = new Label { Font = font, ForeColor = color, AutoSize = true, BackColor = parent.BackColor, UseMnemonic = false };
            parent.Controls.Add(l);
            return l;
        }

        Panel MkPage(bool scroll)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, AutoScroll = scroll, Visible = false };
            if (scroll) p.HandleCreated += delegate
            {
                if (Theme.IsDark) Native.DarkScrollbars(p.Handle);
                else Native.SetWindowTheme(p.Handle, "Explorer", null);
            };
            content.Controls.Add(p);
            return p;
        }

        void SetupTooltips()
        {
            tips.OwnerDraw = true;
            tips.InitialDelay = 350;
            tips.Draw += delegate(object s, DrawToolTipEventArgs e)
            {
                e.Graphics.Clear(Theme.MenuBg);
                using (var pen = new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
                TextRenderer.DrawText(e.Graphics, e.ToolTipText, Theme.Small, e.Bounds, Theme.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            };
            tips.Popup += delegate(object s, PopupEventArgs e)
            {
                Size sz = TextRenderer.MeasureText(tips.GetToolTip(e.AssociatedControl), Theme.Small);
                e.ToolTipSize = new Size(sz.Width + D(18), sz.Height + D(10));
            };
        }

        void BuildSidebar()
        {
            side = new Panel { Dock = DockStyle.Left, Width = D(SideW), BackColor = Theme.Side };
            Controls.Add(side);

            sideLogo = new PictureBox { Image = IconArt.Draw(D(28), Theme.IsDark), Size = new Size(D(28), D(28)), BackColor = Theme.Side };
            side.Controls.Add(sideLogo);
            sideBrand = MkLabel(side, Theme.Brand, Theme.Text);
            sideBrand.Text = "iStream";

            navStream = MkNav(Icons.Display, 0);
            navSettings = MkNav(Icons.Sliders, 1);
            navHelp = MkNav(Icons.Help, 2);

            sideDot = new StatusDot { Size = new Size(D(9), D(9)), BackColor = Theme.Side };
            side.Controls.Add(sideDot);
            sideStatus = MkLabel(side, Theme.Small, Theme.Muted);
            sideShortcut = MkLabel(side, Theme.Tiny, Theme.Faint);
            sideVersion = MkLabel(side, Theme.Tiny, Theme.Faint);
            sideVersion.Text = "iStream 1.1  ·  UxPlay engine";
        }

        FlatButton MkNav(Action<Graphics, RectangleF, Color> glyph, int pageIndex)
        {
            var b = new FlatButton(BtnKind.Nav) { Glyph = glyph, BackColor = Theme.Side };
            b.Click += delegate { ShowPage(pageIndex); };
            side.Controls.Add(b);
            return b;
        }

        void BuildStreamPage()
        {
            pageStream = MkPage(false);
            dot = new StatusDot { Size = new Size(D(12), D(12)), BackColor = Theme.Bg };
            pageStream.Controls.Add(dot);
            lblTitle = MkLabel(pageStream, Theme.H1, Theme.Text);
            lblHint = MkLabel(pageStream, Theme.Body, Theme.Muted);

            fwBanner = new CardPanel { Visible = false };
            fwBanner.SetColors(Theme.WarnTint, Theme.WarnBorder);
            pageStream.Controls.Add(fwBanner);
            fwIcon = new IconBox { Painter = Icons.Warning, IconColor = Theme.Warn, BackColor = Theme.WarnTint };
            fwBanner.Controls.Add(fwIcon);
            lblFw = MkLabel(fwBanner, Theme.Body, Theme.Text);
            btnFw = new FlatButton(BtnKind.Secondary) { BackColor = Theme.WarnTint };
            btnFw.Click += delegate { FixFirewall(); };
            fwBanner.Controls.Add(btnFw);

            pendingBanner = new CardPanel { Visible = false };
            pendingBanner.SetColors(Theme.Card, Theme.Border);
            pageStream.Controls.Add(pendingBanner);
            pendIcon = new IconBox { Painter = Icons.Info, IconColor = Theme.Text, BackColor = Theme.Card };
            pendingBanner.Controls.Add(pendIcon);
            lblPendTitle = MkLabel(pendingBanner, Theme.BodySemi, Theme.Text);
            lblPendBody = MkLabel(pendingBanner, Theme.Small, Theme.Muted);
            btnApplyNow = new FlatButton(BtnKind.Secondary) { BackColor = Theme.Card };
            btnApplyNow.Click += delegate { ApplyNow(); };
            pendingBanner.Controls.Add(btnApplyNow);

            btnPower = new FlatButton(BtnKind.Primary) { Glyph = Icons.Power, BackColor = Theme.Bg };
            btnPower.Click += delegate { TogglePower(); };
            pageStream.Controls.Add(btnPower);
            btnFullscreen = new FlatButton(BtnKind.Secondary) { Glyph = Icons.Expand, BackColor = Theme.Bg };
            btnFullscreen.Click += delegate { ToggleFullscreen(); };
            pageStream.Controls.Add(btnFullscreen);

            lblRotation = MkLabel(pageStream, Theme.Section, Theme.Text);
            rotCard = new CardPanel();
            rotCard.SetColors(Theme.Card, Theme.BorderSoft);
            pageStream.Controls.Add(rotCard);
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var t = new FlatButton(BtnKind.Tile) { BackColor = Theme.Card };
                t.Glyph = delegate(Graphics g, RectangleF r, Color c) { Icons.Rotation(g, r, idx, Theme.Text, Theme.Accent); };
                t.Click += delegate { SetRotation(idx); };
                rotTiles[i] = t;
                rotCard.Controls.Add(t);
            }

            lblSetup = MkLabel(pageStream, Theme.Section, Theme.Text);
        }

        void BuildSettingsPage()
        {
            pageSettings = MkPage(true);
            setTitle = MkLabel(pageSettings, Theme.H1, Theme.Text);

            Section q = AddSection("sec_quality");
            selQuality = new SelectBox(); AddRow(q, "lbl_resolution", "desc_resolution", selQuality);
            selFps = new SelectBox(); AddRow(q, "lbl_fps", "desc_fps", selFps);
            selRenderer = new SelectBox(); AddRow(q, "lbl_renderer", "desc_renderer", selRenderer);

            Section p = AddSection("sec_playback");
            tglLowLat = new ToggleSwitch(); AddRow(p, "lbl_lowlat", "desc_lowlat", tglLowLat);
            tglAudio = new ToggleSwitch(); AddRow(p, "lbl_audio", "desc_audio", tglAudio);
            tglAutoFs = new ToggleSwitch(); AddRow(p, "lbl_autofs", "desc_autofs", tglAutoFs);
            selMonitor = new SelectBox(); AddRow(p, "lbl_monitor", "desc_monitor", selMonitor);

            Section g = AddSection("sec_general");
            txtName = new TextField(); AddRow(g, "lbl_name", "desc_name", txtName);
            selLang = new SelectBox(); AddRow(g, "lbl_lang", "desc_lang", selLang);
            selTheme = new SelectBox(); AddRow(g, "lbl_theme", "desc_theme", selTheme);
            tglAutoStart = new ToggleSwitch(); AddRow(g, "lbl_autostart", "desc_autostart", tglAutoStart);
            tglWinStart = new ToggleSwitch(); AddRow(g, "lbl_winstart", "desc_winstart", tglWinStart);

            selQuality.SelectedIndexChanged += delegate { if (loadingUi) return; settings.Quality = selQuality.SelectedIndex; EngineSettingChanged(); };
            selFps.SelectedIndexChanged += delegate { if (loadingUi) return; settings.Fps = selFps.SelectedIndex == 1 ? 30 : 60; EngineSettingChanged(); };
            selRenderer.SelectedIndexChanged += delegate { if (loadingUi) return; settings.Renderer = selRenderer.SelectedIndex; EngineSettingChanged(); };
            tglLowLat.CheckedChanged += delegate { if (loadingUi) return; settings.LowLatency = tglLowLat.Checked; EngineSettingChanged(); };
            tglAudio.CheckedChanged += delegate { if (loadingUi) return; settings.Audio = tglAudio.Checked; EngineSettingChanged(); };
            tglAutoFs.CheckedChanged += delegate { if (loadingUi) return; settings.AutoFullscreen = tglAutoFs.Checked; settings.Save(); };
            selMonitor.SelectedIndexChanged += delegate { if (loadingUi) return; settings.Monitor = selMonitor.SelectedIndex - 1; settings.Save(); };
            tglAutoStart.CheckedChanged += delegate { if (loadingUi) return; settings.AutoStart = tglAutoStart.Checked; settings.Save(); };
            tglWinStart.CheckedChanged += delegate { if (loadingUi) return; SetWinStart(tglWinStart.Checked); };
            selLang.SelectedIndexChanged += delegate
            {
                if (loadingUi) return;
                settings.Language = L.Codes[Math.Max(0, selLang.SelectedIndex)];
                L.Lang = settings.Language;
                settings.Save();
                ApplyTexts();
            };
            selTheme.SelectedIndexChanged += delegate
            {
                if (loadingUi) return;
                settings.ThemeMode = Math.Max(0, selTheme.SelectedIndex);
                settings.Save();
                BeginInvoke(new Action(ApplyTheme));   // after the dropdown has closed
            };
            txtName.Box.MaxLength = 40;
            txtName.Box.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitName(); pageSettings.Focus(); }
                else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; txtName.Box.Text = settings.DeviceName; }
            };
            txtName.Box.LostFocus += delegate { CommitName(); };
        }

        Section AddSection(string key)
        {
            var s = new Section { Key = key };
            s.Header = MkLabel(pageSettings, Theme.Section, Theme.Text);
            s.Card = new CardPanel();
            s.Card.SetColors(Theme.Card, Theme.BorderSoft);
            pageSettings.Controls.Add(s.Card);
            sections.Add(s);
            return s;
        }

        void AddRow(Section s, string titleKey, string descKey, Control ctl)
        {
            var r = new Row { TitleKey = titleKey, DescKey = descKey, Ctl = ctl };
            r.Title = MkLabel(s.Card, Theme.Body, Theme.Text);
            r.Desc = MkLabel(s.Card, Theme.Small, Theme.Muted);
            ctl.BackColor = Theme.Card;
            s.Card.Controls.Add(ctl);
            if (s.Rows.Count > 0)
            {
                r.Sep = new Panel { BackColor = Theme.BorderSoft, Height = 1 };
                s.Card.Controls.Add(r.Sep);
            }
            s.Rows.Add(r);
        }

        void BuildHelpPage()
        {
            pageHelp = MkPage(true);
            helpTitle = MkLabel(pageHelp, Theme.H1, Theme.Text);
            foreach (string key in new[] { "help_connect", "help_shortcuts", "help_trouble", "help_about" })
            {
                var h = new HelpCard { Key = key, Card = new CardPanel() };
                h.Card.SetColors(Theme.Card, Theme.BorderSoft);
                pageHelp.Controls.Add(h.Card);
                h.Title = MkLabel(h.Card, Theme.BodySemi, Theme.Text);
                h.Body = MkLabel(h.Card, Theme.Body, Theme.Muted);
                helpCards.Add(h);
            }
        }

        // ---------------------------------------------------------- texts & layout

        void ApplyTexts()
        {
            loadingUi = true;
            navStream.Text = L.T("nav_stream");
            navSettings.Text = L.T("nav_settings");
            navHelp.Text = L.T("nav_help");
            sideShortcut.Text = !hk1 || !hk2 ? L.F("shortcut_busy", !hk1 ? "F" : "R") : "Ctrl+Alt+F  ·  Ctrl+Alt+R";

            btnFw.Text = fwFixing ? L.T("fw_fixing") : L.T("fw_fix");
            lblFw.Text = fwState == Firewall.State.Blocked ? L.T("fw_blocked") : L.T("fw_norule");
            lblPendTitle.Text = L.T("pending_title");
            lblPendBody.Text = L.T("pending_body");
            btnApplyNow.Text = L.T("apply_now");
            tips.SetToolTip(btnApplyNow, L.T("apply_now_tip"));
            lblRotation.Text = L.T("rot_title");
            for (int i = 0; i < 4; i++) tips.SetToolTip(rotTiles[i], L.T("rot_" + i));
            lblSetup.Text = L.T("setup_title");

            setTitle.Text = L.T("nav_settings");
            foreach (Section s in sections)
            {
                s.Header.Text = L.T(s.Key);
                foreach (Row r in s.Rows)
                {
                    r.Title.Text = L.T(r.TitleKey);
                    r.Desc.Text = L.T(r.DescKey);
                }
            }
            selQuality.SetItems(new[] { L.T("q0"), L.T("q1"), L.T("q2"), L.T("q3") }, settings.Quality);
            selFps.SetItems(new[] { L.T("fps60"), L.T("fps30") }, settings.Fps == 30 ? 1 : 0);
            selRenderer.SetItems(new[] { L.T("r0"), "Direct3D 12" }, settings.Renderer);
            var mons = new List<string> { L.T("mon_auto") };
            Screen[] all = Screen.AllScreens;
            for (int i = 0; i < all.Length; i++)
                mons.Add(L.F("mon_n", i + 1, all[i].Bounds.Width, all[i].Bounds.Height) + (all[i].Primary ? L.T("mon_primary") : ""));
            int mi = settings.Monitor + 1;
            selMonitor.SetItems(mons, mi >= 0 && mi < mons.Count ? mi : 0);
            selLang.SetItems(L.Names, Math.Max(0, Array.IndexOf(L.Codes, L.Lang)));
            selTheme.SetItems(new[] { L.T("th0"), L.T("th1"), L.T("th2") }, settings.ThemeMode);
            tglLowLat.SetSilently(settings.LowLatency);
            tglAudio.SetSilently(settings.Audio);
            tglAutoFs.SetSilently(settings.AutoFullscreen);
            tglAutoStart.SetSilently(settings.AutoStart);
            tglWinStart.SetSilently(GetWinStart());
            if (!txtName.Box.Focused) txtName.Box.Text = settings.DeviceName;

            helpTitle.Text = L.T("nav_help");
            foreach (HelpCard h in helpCards)
            {
                h.Title.Text = L.T(h.Key + "_t");
                h.Body.Text = L.F(h.Key + "_b", settings.DeviceName);
            }
            loadingUi = false;

            UpdateRotationTiles();
            RefreshStatus(true);
            LayoutAll();
        }

        void LayoutAll()
        {
            LayoutSidebar();
            LayoutStream();
            LayoutSettings();
            LayoutHelp();
        }

        void LayoutSidebar()
        {
            int w = side.Width, h = ClientSize.Height;
            sideLogo.Location = new Point(D(20), D(22));
            sideBrand.Location = new Point(D(58), sideLogo.Top + (sideLogo.Height - sideBrand.Height) / 2);
            int y = D(82);
            foreach (FlatButton b in new[] { navStream, navSettings, navHelp })
            {
                b.SetBounds(D(10), y, w - D(20), D(38));
                y += D(40);
            }
            sideVersion.Location = new Point(D(20), h - D(22) - sideVersion.Height);
            sideShortcut.MaximumSize = new Size(w - D(40), 0);
            sideShortcut.Location = new Point(D(20), sideVersion.Top - D(4) - sideShortcut.Height);
            sideStatus.Location = new Point(D(36), sideShortcut.Top - D(14) - sideStatus.Height);
            sideDot.Location = new Point(D(20), sideStatus.Top + (sideStatus.Height - sideDot.Height) / 2);
        }

        void LayoutStream()
        {
            if (pageStream == null) return;
            int x0 = D(48), cw = D(ClientW - SideW) - 2 * x0;
            int y = D(44);
            lblTitle.Location = new Point(x0 + D(24), y);
            dot.Location = new Point(x0, y + (lblTitle.Height - dot.Height) / 2 + D(1));
            y += lblTitle.Height + D(6);
            lblHint.MaximumSize = new Size(cw, 0);
            lblHint.Location = new Point(x0, y);
            y += Math.Max(lblHint.Height, D(40)) + D(22);

            bool anyBanner = false;
            if (fwBanner.Visible) { y = LayoutBanner(fwBanner, fwIcon, lblFw, null, btnFw, x0, y, cw) + D(10); anyBanner = true; }
            if (pendingBanner.Visible) { y = LayoutBanner(pendingBanner, pendIcon, lblPendTitle, lblPendBody, btnApplyNow, x0, y, cw) + D(10); anyBanner = true; }
            if (anyBanner) y += D(10);

            int bw = (cw - D(12)) / 2, bh = D(44);
            btnPower.SetBounds(x0, y, bw, bh);
            btnFullscreen.SetBounds(x0 + bw + D(12), y, bw, bh);
            y += bh + D(28);

            lblRotation.Location = new Point(x0, y);
            y += lblRotation.Height + D(10);
            int tile = D(84), gap = D(14), pad = D(16);
            rotCard.SetBounds(x0, y, cw, tile + 2 * pad);
            int tx = (cw - (4 * tile + 3 * gap)) / 2;
            for (int i = 0; i < 4; i++) { rotTiles[i].SetBounds(tx, pad, tile, tile); tx += tile + gap; }
            y += rotCard.Height + D(28);

            lblSetup.Location = new Point(x0, y);
            y += lblSetup.Height + D(10);
            LayoutChips(x0, y, cw);
        }

        int LayoutBanner(CardPanel card, IconBox icon, Label title, Label body, FlatButton btn, int x, int y, int w)
        {
            int pad = D(16), isz = D(20);
            int bwid = Math.Max(D(124), TextRenderer.MeasureText(btn.Text, btn.Font).Width + D(32)), bhei = D(36);
            int textX = pad + isz + D(12);
            int textW = w - textX - bwid - pad * 2;
            title.MaximumSize = new Size(textW, 0);
            int th = title.Height;
            if (body != null)
            {
                body.MaximumSize = new Size(textW, 0);
                th += D(3) + body.Height;
            }
            int h = Math.Max(th, bhei) + pad * 2;
            int ty = (h - th) / 2;
            title.Location = new Point(textX, ty);
            if (body != null) body.Location = new Point(textX, ty + title.Height + D(3));
            icon.SetBounds(pad, ty + (title.Font.Height - isz) / 2 + D(1), isz, isz);
            btn.SetBounds(w - pad - bwid, (h - bhei) / 2, bwid, bhei);
            card.SetBounds(x, y, w, h);
            return y + h;
        }

        void LayoutChips(int x0, int y, int cw)
        {
            foreach (FlatButton c in chips) { pageStream.Controls.Remove(c); c.Dispose(); }
            chips.Clear();
            var texts = new List<string>();
            texts.Add(settings.DeviceName);
            texts.Add(Engine.ShortQuality[settings.Quality]);
            texts.Add(settings.Fps.ToString(CultureInfo.InvariantCulture) + " FPS");
            texts.Add(settings.Renderer == 1 ? "Direct3D 12" : "Direct3D 11");
            if (settings.LowLatency) texts.Add(L.T("chip_lowlat"));
            if (!settings.Audio) texts.Add(L.T("chip_noaudio"));
            int x = x0;
            foreach (string t in texts)
            {
                var c = new FlatButton(BtnKind.Chip) { Text = t, BackColor = Theme.Bg, TabStop = false };
                int w = TextRenderer.MeasureText(t, c.Font).Width + D(26);
                if (x + w > x0 + cw) { x = x0; y += D(38); }
                c.SetBounds(x, y, w, D(30));
                c.Click += delegate { ShowPage(1); };
                pageStream.Controls.Add(c);
                chips.Add(c);
                x += w + D(8);
            }
        }

        void LayoutSettings()
        {
            if (pageSettings == null) return;
            int scroll = pageSettings.AutoScrollPosition.Y;
            int x0 = D(44);
            int cw = D(ClientW - SideW) - x0 - D(26) - SystemInformation.VerticalScrollBarWidth;
            int y = D(44);
            setTitle.Location = new Point(x0, y + scroll);
            y += setTitle.Height + D(26);
            foreach (Section s in sections)
            {
                s.Header.Location = new Point(x0, y + scroll);
                y += s.Header.Height + D(10);
                int pad = D(18), ry = 0;
                foreach (Row r in s.Rows)
                {
                    bool toggle = r.Ctl is ToggleSwitch;
                    int ctlW = toggle ? D(40) : D(236), ctlH = toggle ? D(22) : D(34);
                    int textW = cw - pad * 3 - ctlW;
                    r.Title.MaximumSize = new Size(textW, 0);
                    r.Desc.MaximumSize = new Size(textW, 0);
                    int block = r.Title.Height + D(2) + r.Desc.Height;
                    int rh = Math.Max(D(66), block + D(28));
                    int ty = ry + (rh - block) / 2;
                    r.Title.Location = new Point(pad, ty);
                    r.Desc.Location = new Point(pad, ty + r.Title.Height + D(2));
                    r.Ctl.SetBounds(cw - pad - ctlW, ry + (rh - ctlH) / 2, ctlW, ctlH);
                    if (r.Sep != null) r.Sep.SetBounds(pad, ry, cw - 2 * pad, 1);
                    ry += rh;
                }
                s.Card.SetBounds(x0, y + scroll, cw, ry);
                y += ry + D(30);
            }
            pageSettings.AutoScrollMinSize = new Size(0, y + D(10));
        }

        void LayoutHelp()
        {
            if (pageHelp == null) return;
            int scroll = pageHelp.AutoScrollPosition.Y;
            int x0 = D(44);
            int cw = D(ClientW - SideW) - x0 - D(26) - SystemInformation.VerticalScrollBarWidth;
            int y = D(44);
            helpTitle.Location = new Point(x0, y + scroll);
            y += helpTitle.Height + D(26);
            foreach (HelpCard h in helpCards)
            {
                int pad = D(20);
                h.Title.Location = new Point(pad, pad);
                h.Body.MaximumSize = new Size(cw - 2 * pad, 0);
                h.Body.Location = new Point(pad, h.Title.Bottom + D(8));
                h.Card.SetBounds(x0, y + scroll, cw, h.Body.Bottom + pad);
                y += h.Card.Height + D(14);
            }
            pageHelp.AutoScrollMinSize = new Size(0, y + D(20));
        }

        void ShowPage(int i)
        {
            if (page == i) return;
            page = i;
            pageStream.Visible = i == 0;
            pageSettings.Visible = i == 1;
            pageHelp.Visible = i == 2;
            navStream.Selected = i == 0;
            navSettings.Selected = i == 1;
            navHelp.Selected = i == 2;
        }

        void UpdateRotationTiles()
        {
            for (int i = 0; i < 4; i++) rotTiles[i].Selected = i == settings.Rotation;
        }

        // ---------------------------------------------------------- lifecycle

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowChrome();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ShowPage(0);
            ApplyTexts();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (shotDir != null) { TakeScreenshots(); return; }

            hk1 = Native.RegisterHotKey(Handle, HK_FULLSCREEN, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, (uint)Keys.F);
            hk2 = Native.RegisterHotKey(Handle, HK_ROTATE, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, (uint)Keys.R);
            if (!hk1 || !hk2) ApplyTexts();

            if (!engine.Installed)
            {
                status = Status.Missing;
                RefreshStatus(true);
                return;
            }

            if (File.Exists(Engine.MachineArgumentsPath))
                MessageBox.Show(this, L.F("machine_override", Engine.MachineArgumentsPath), "iStream",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            CheckFirewallAsync();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;   // Windows light/dark switch
            if (settings.AutoStart) StartEngine();
            timer.Start();
            Tick();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            closing = true;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            timer.Stop();
            Native.UnregisterHotKey(Handle, HK_FULLSCREEN);
            Native.UnregisterHotKey(Handle, HK_ROTATE);
            if (shotDir == null && status != Status.Missing)
            {
                wantRunning = false;
                try { if (opTask != null) opTask.Wait(6000); } catch { }   // don't let a pending start outlive us
                try { engine.Stop(); } catch { }
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)   // WM_HOTKEY
            {
                int id = m.WParam.ToInt32();
                if (id == HK_FULLSCREEN) ToggleFullscreen();
                else if (id == HK_ROTATE) SetRotation((settings.Rotation + 1) % 4);
            }
            base.WndProc(ref m);
        }

        // ---------------------------------------------------------- status

        void Tick()
        {
            if (closing || status == Status.Missing) return;
            if (busy) { RefreshStatus(false); return; }

            int pid = engine.RunningPid();
            if (pid == 0 && wantRunning && unchecked(Environment.TickCount - lastAutoStart) > 4000)
            {
                StartEngine();   // engine died but should be running — bring it back
                return;
            }

            bool streaming = video.Update(pid, settings.AutoFullscreen, PickScreen);
            if (streaming)
            {
                lastWindowSeen = Environment.TickCount;
                Decorate(video.Handle);
            }

            // Saved changes go live as soon as nobody is connected (never mid-stream).
            if (pending && pid != 0 && !streaming && unchecked(Environment.TickCount - lastWindowSeen) > 2000)
            {
                StartEngine();
                return;
            }
            if (pending && pid == 0 && !wantRunning) pending = false;   // next start uses the saved settings

            status = pid == 0 ? Status.Stopped : streaming ? Status.Streaming : Status.Ready;
            RefreshStatus(false);
        }

        void RefreshStatus(bool force)
        {
            string title, hint, shortText;
            Color c;
            switch (status)
            {
                case Status.Missing:
                    c = Theme.Bad; title = L.T("engine_missing_t"); hint = L.T("engine_missing_b"); shortText = L.T("s_stopped"); break;
                case Status.Busy:
                    c = Theme.Warn; title = L.T(busyKey); hint = lblHint.Text; shortText = L.T("s_busy"); break;
                case Status.Streaming:
                    c = Theme.Good; title = L.T("status_streaming"); hint = L.T("hint_streaming"); shortText = L.T("s_streaming"); break;
                case Status.Ready:
                    c = Theme.Accent; title = L.T("status_ready"); hint = L.F("hint_ready", "“" + settings.DeviceName + "”"); shortText = L.T("s_ready"); break;
                default:
                    c = Theme.Faint; title = L.T("status_stopped"); hint = L.T("hint_stopped"); shortText = L.T("s_stopped"); break;
            }
            if (status == Status.Busy && force) hint = L.T("hint_stopped");
            if (transientHint != null && unchecked(Environment.TickCount - transientUntil) < 0) hint = transientHint;

            bool relayout = force;
            if (lblTitle.Text != title) { lblTitle.Text = title; relayout = true; }
            if (lblHint.Text != hint) { lblHint.Text = hint; relayout = true; }
            dot.DotColor = c;
            sideDot.DotColor = c;
            if (sideStatus.Text != shortText) sideStatus.Text = shortText;

            bool running = status == Status.Ready || status == Status.Streaming || wantRunning;
            btnPower.Text = L.T(running ? "btn_stop" : "btn_start");
            btnPower.Enabled = !busy && status != Status.Missing;
            bool streaming = status == Status.Streaming;
            btnFullscreen.Enabled = streaming;
            btnFullscreen.Text = L.T(streaming && video.IsFullscreen ? "btn_exit_fullscreen" : "btn_fullscreen");

            bool showPending = pending && streaming;
            if (pendingBanner.Visible != showPending) { pendingBanner.Visible = showPending; relayout = true; }
            bool showFw = fwState == Firewall.State.Blocked || fwState == Firewall.State.NoRule;
            if (fwBanner.Visible != showFw) { fwBanner.Visible = showFw; relayout = true; }

            if (relayout && !force) LayoutStream();
        }

        void ShowHint(string text, int ms)
        {
            transientHint = text;
            transientUntil = unchecked(Environment.TickCount + ms);
            RefreshStatus(false);
        }

        Screen PickScreen(IntPtr h)
        {
            Screen[] all = Screen.AllScreens;
            if (settings.Monitor >= 0 && settings.Monitor < all.Length) return all[settings.Monitor];
            return Screen.FromHandle(h);
        }

        void Decorate(IntPtr h)
        {
            if (h == decorated) return;
            decorated = h;
            Native.SetWindowText(h, VideoTitle);
            Native.SendMessage(h, 0x80, (IntPtr)0, iconSmall.Handle);   // WM_SETICON small
            Native.SendMessage(h, 0x80, (IntPtr)1, iconBig.Handle);     // WM_SETICON big
        }

        // ---------------------------------------------------------- actions

        void TogglePower()
        {
            if (busy) return;
            if (wantRunning || engine.RunningPid() != 0) StopEngine(); else StartEngine();
        }

        void StartEngine()
        {
            wantRunning = true;
            pending = false;
            Settings snap = settings.Clone();
            RunOp(delegate { engine.Start(snap); }, "status_starting");
        }

        void StopEngine()
        {
            wantRunning = false;
            pending = false;
            RunOp(delegate { engine.Stop(); }, "status_stopping");
        }

        void RunOp(Action op, string key)
        {
            if (closing) return;
            busy = true;
            busyKey = key;
            lastAutoStart = Environment.TickCount;
            status = Status.Busy;
            RefreshStatus(false);
            opTask = Task.Run(op);
            opTask.ContinueWith(delegate(Task t)
            {
                busy = false;
                lastAutoStart = Environment.TickCount;
                if (closing) return;
                if (t.Exception != null)
                {
                    wantRunning = false;
                    MessageBox.Show(this, L.T("err_start") + "\n" + t.Exception.GetBaseException().Message,
                                    "iStream", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Tick();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // Engine-level setting changed: saved now, applied when no stream is running.
        void EngineSettingChanged()
        {
            settings.Save();
            LayoutStream();   // chips
            if (wantRunning || engine.RunningPid() != 0)
            {
                pending = true;
                Tick();
            }
        }

        void ApplyNow()
        {
            if (busy) return;
            StartEngine();
            ShowHint(L.T("hint_reconnect"), 8000);
        }

        void ToggleFullscreen()
        {
            if (video.Handle == IntPtr.Zero) { ShowHint(L.T("hint_connect_first"), 3500); return; }
            video.Toggle(PickScreen(video.Handle));
            RefreshStatus(false);
        }

        void SetRotation(int r)
        {
            if (status == Status.Missing) return;
            settings.Rotation = r;
            UpdateRotationTiles();
            EngineSettingChanged();
        }

        void CommitName()
        {
            if (loadingUi) return;
            string n = Engine.SafeName(txtName.Box.Text);
            if (txtName.Box.Text != n) txtName.Box.Text = n;
            if (n == settings.DeviceName) return;
            settings.DeviceName = n;
            EngineSettingChanged();
            ApplyTexts();
        }

        static bool GetWinStart()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue("iStream") != null;
            }
            catch { return false; }
        }

        static void SetWinStart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (on) k.SetValue("iStream", "\"" + Application.ExecutablePath + "\" --minimized");
                    else k.DeleteValue("iStream", false);
                }
            }
            catch { }
        }

        void CheckFirewallAsync()
        {
            string exe = engine.ExePath;
            Task.Run(() => Firewall.Check(exe)).ContinueWith(delegate(Task<Firewall.State> t)
            {
                if (closing) return;
                fwState = t.Result;
                lblFw.Text = fwState == Firewall.State.Blocked ? L.T("fw_blocked") : L.T("fw_norule");
                RefreshStatus(false);
                LayoutStream();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void FixFirewall()
        {
            if (fwFixing) return;
            DialogResult answer = MessageBox.Show(this, L.T("fw_confirm"), L.T("fw_fix"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            fwFixing = true;
            btnFw.Text = L.T("fw_fixing");
            btnFw.Enabled = false;
            string exe = engine.ExePath;
            Task.Run(() => Firewall.Fix(exe)).ContinueWith(delegate(Task<bool> t)
            {
                fwFixing = false;
                if (closing) return;
                btnFw.Enabled = true;
                btnFw.Text = L.T("fw_fix");
                if (!t.Result)
                    MessageBox.Show(this, L.T("fw_failed"), "iStream", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    ShowHint(L.T("hint_fw_done"), 6000);
                CheckFirewallAsync();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        // ---------------------------------------------------------- screenshots (development)

        void TakeScreenshots()
        {
            Directory.CreateDirectory(shotDir);
            fwState = Firewall.Check(engine.ExePath);
            lblFw.Text = fwState == Firewall.State.Blocked ? L.T("fw_blocked") : L.T("fw_norule");
            status = Status.Ready;
            RefreshStatus(true);
            LayoutStream();
            Snap("1-stream-ready.png");

            status = Status.Streaming;
            pending = true;
            settings.Rotation = 1;
            UpdateRotationTiles();
            RefreshStatus(true);
            LayoutStream();
            Snap("2-stream-pending.png");

            ShowPage(1);
            Snap("3-settings.png");
            pageSettings.AutoScrollPosition = new Point(0, D(2000));
            Snap("4-settings-bottom.png");
            ShowPage(2);
            Snap("5-help.png");
            Close();
        }

        void Snap(string name)
        {
            TopMost = true;
            Activate();
            for (int i = 0; i < 4; i++) { Application.DoEvents(); Thread.Sleep(60); }
            // prawdziwy zrzut ekranu — pokazuje to, co widzi użytkownik (ciemny pasek tytułu, paski przewijania)
            Point origin = PointToScreen(Point.Empty);
            Rectangle area = new Rectangle(origin.X, origin.Y - SystemInformation.CaptionHeight - D(8),
                                           ClientSize.Width, ClientSize.Height + SystemInformation.CaptionHeight + D(8));
            using (var bmp = new Bitmap(area.Width, area.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(area.Location, Point.Empty, area.Size);
                bmp.Save(Path.Combine(shotDir, name), ImageFormat.Png);
            }
        }
    }
}
