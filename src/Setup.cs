// iStream installer and uninstaller.
// Built twice by build-setup.cmd:
//   uninstall.exe        - this code without a payload  -> uninstall mode
//   iStream-Setup-x.exe  - this code + payload.zip + uninstall.exe as resources -> install mode
// Installs per user (no administrator rights): %LOCALAPPDATA%\Programs\iStream by default.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("iStream Setup")]
[assembly: AssemblyProduct("iStream")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

namespace IStream
{
    static class Native
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);
        public static void Dwm(IntPtr h, int attr, int value) { try { DwmSetWindowAttribute(h, attr, ref value, 4); } catch { } }
        public static void RoundCorners(IntPtr h) { Dwm(h, 33, 3); }
    }

    static class Product
    {
        public const string Name = "iStream";
        public const string Version = "1.3.0";
        public const string Publisher = "olehnykorovych";
        public const string Url = "https://github.com/olehnykorovych/iStream-AirPlay";
        public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\iStream";
        public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static readonly string[] Processes = { "iStream", "uxplay-windows", "uxplay-bluetooth-beacon" };
        // Everything the installer puts in the folder; the uninstaller removes only these.
        public static readonly string[] Files = { "iStream.exe", "LICENSE", "THIRD_PARTY_NOTICES.md", "README.txt" };
        public const string EngineDir = "engine";
        public const string UninstallExe = "uninstall.exe";

        public static string DefaultDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", Name); }
        }

        public static string StartMenuLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Name + ".lnk"); }
        }

        public static string DesktopLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Name + ".lnk"); }
        }

        public static string SettingsDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name); }
        }

        public static string InstalledDir()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(UninstallKey))
                {
                    string d = k == null ? null : k.GetValue("InstallLocation") as string;
                    return string.IsNullOrEmpty(d) ? null : d;
                }
            }
            catch { return null; }
        }
    }

    static class T
    {
        static readonly bool Pl = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pl";
        static readonly Dictionary<string, string[]> map = new Dictionary<string, string[]>
        {
            { "title",      new[] { "Install iStream", "Zainstaluj iStream" } },
            { "subtitle",   new[] { "AirPlay screen mirroring for Windows · version {0}", "Klonowanie ekranu AirPlay na Windows · wersja {0}" } },
            { "location",   new[] { "Install location", "Folder instalacji" } },
            { "change",     new[] { "Change…", "Zmień…" } },
            { "desktop",    new[] { "Create a desktop shortcut", "Utwórz skrót na pulpicie" } },
            { "launch",     new[] { "Open iStream when setup finishes", "Uruchom iStream po instalacji" } },
            { "peruser",    new[] { "Installs for your Windows account only. No administrator rights needed.", "Instaluje tylko dla Twojego konta Windows. Nie wymaga uprawnień administratora." } },
            { "install",    new[] { "Install", "Zainstaluj" } },
            { "update",     new[] { "Update", "Aktualizuj" } },
            { "cancel",     new[] { "Cancel", "Anuluj" } },
            { "close",      new[] { "Close", "Zamknij" } },
            { "open",       new[] { "Open iStream", "Otwórz iStream" } },
            { "copying",    new[] { "Copying files… {0}%", "Kopiowanie plików… {0}%" } },
            { "finishing",  new[] { "Creating shortcuts…", "Tworzenie skrótów…" } },
            { "done_t",     new[] { "iStream is installed", "iStream jest zainstalowany" } },
            { "done_b",     new[] { "Find it in the Start menu. To remove it later, use Settings → Apps → Installed apps.", "Znajdziesz go w menu Start. Usuniesz go w Ustawienia → Aplikacje → Zainstalowane aplikacje." } },
            { "pick",       new[] { "Choose where to install iStream. A folder named iStream will be created there.", "Wybierz, gdzie zainstalować iStream. Zostanie tam utworzony folder iStream." } },
            { "running",    new[] { "iStream is running. Close it and continue?", "iStream jest uruchomiony. Zamknąć go i kontynuować?" } },
            { "noaccess",   new[] { "Setup can't write to this folder:\n{0}\n\nChoose a folder inside your user profile, or run setup as administrator.", "Instalator nie może zapisać w tym folderze:\n{0}\n\nWybierz folder w swoim profilu użytkownika albo uruchom instalator jako administrator." } },
            { "failed",     new[] { "Setup failed:\n{0}", "Instalacja nie powiodła się:\n{0}" } },
            { "u_title",    new[] { "Uninstall iStream", "Odinstaluj iStream" } },
            { "u_body",     new[] { "Removes iStream from this folder, along with its shortcuts and its start-up entry:", "Usuwa iStream z tego folderu razem ze skrótami i wpisem autostartu:" } },
            { "u_settings", new[] { "Also delete my iStream settings", "Usuń też moje ustawienia iStream" } },
            { "u_button",   new[] { "Uninstall", "Odinstaluj" } },
            { "u_working",  new[] { "Removing iStream…", "Usuwanie iStream…" } },
            { "u_done_t",   new[] { "iStream was removed", "iStream został usunięty" } },
            { "u_done_b",   new[] { "Thanks for trying it.", "Dzięki, że go wypróbowałeś." } },
            { "u_notfound", new[] { "iStream isn't installed in this folder, so there is nothing to remove.", "W tym folderze nie ma zainstalowanego iStream, więc nie ma czego usuwać." } },
        };

        public static string S(string key) { string[] v; return map.TryGetValue(key, out v) ? v[Pl ? 1 : 0] : key; }
        public static string F(string key, object a) { return string.Format(S(key), a); }
    }

    static class SetupProgram
    {
        [STAThread]
        static int Main(string[] args)
        {
            bool quiet = false, noDesktop = false, noLaunch = false, purge = false;
            string dir = null;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--quiet" || a == "/s" || a == "/quiet") quiet = true;
                else if (a == "--no-desktop") noDesktop = true;
                else if (a == "--no-launch") noLaunch = true;
                else if (a == "--purge") purge = true;
                else if (a == "--dir" && i + 1 < args.Length) dir = args[++i];
            }

            bool installMode = Payload.Has("payload.zip");

            if (quiet)
            {
                try
                {
                    if (installMode)
                    {
                        Installer.KillRunning(null);
                        Installer.Install(dir ?? Product.InstalledDir() ?? Product.DefaultDir, !noDesktop, null);
                        if (!noLaunch) Installer.Launch(dir ?? Product.InstalledDir() ?? Product.DefaultDir);
                    }
                    else
                    {
                        string here = AppDomain.CurrentDomain.BaseDirectory;
                        if (!Uninstaller.LooksInstalled(here)) return 2;
                        Uninstaller.Run(here, purge);
                    }
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    return 1;
                }
            }

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Gfx.Scale = g.DpiX / 96f;
            Theme.Apply(Installer.PreferredTheme());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm(installMode));
            return 0;
        }
    }

    static class Payload
    {
        public static bool Has(string name)
        {
            return Array.IndexOf(Assembly.GetExecutingAssembly().GetManifestResourceNames(), name) >= 0;
        }

        public static Stream Open(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        }
    }

    static class Installer
    {
        // Same appearance as the app: its saved setting, otherwise Windows' light/dark mode.
        public static int PreferredTheme()
        {
            try
            {
                string ini = Path.Combine(Product.SettingsDir, "settings.ini");
                if (File.Exists(ini))
                    foreach (string line in File.ReadAllLines(ini))
                    {
                        string t = line.Trim();
                        if (t == "theme=1") return Theme.Light;
                        if (t == "theme=2") return Theme.Dark;
                        if (t == "theme=3") return Theme.Gray;
                    }
            }
            catch { }
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object v = k == null ? null : k.GetValue("AppsUseLightTheme");
                    return v is int && (int)v == 0 ? Theme.Dark : Theme.Light;
                }
            }
            catch { return Theme.Dark; }
        }

        static IEnumerable<Process> Running(string onlyUnder)
        {
            foreach (string name in Product.Processes)
                foreach (Process p in Process.GetProcessesByName(name))
                {
                    if (onlyUnder != null)
                    {
                        string path = null;
                        try { path = p.MainModule.FileName; } catch { }
                        if (path == null || !path.StartsWith(onlyUnder, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    yield return p;
                }
        }

        public static bool AnyRunning(string onlyUnder)
        {
            foreach (Process p in Running(onlyUnder)) return true;
            return false;
        }

        public static void KillRunning(string onlyUnder)
        {
            foreach (Process p in new List<Process>(Running(onlyUnder)))
            {
                try { p.Kill(); p.WaitForExit(5000); } catch { }
            }
        }

        public static string Normalize(string dir)
        {
            return Path.GetFullPath(dir).TrimEnd('\\');
        }

        public static bool CanWrite(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".istream-write-test");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        // progress: 0..100, or -1 while creating shortcuts
        public static void Install(string dir, bool desktop, Action<int> progress)
        {
            dir = Normalize(dir);
            Directory.CreateDirectory(dir);
            string root = dir + "\\";

            using (Stream s = Payload.Open("payload.zip"))
            using (var zip = new ZipArchive(s, ZipArchiveMode.Read))
            {
                long total = 0, done = 0;
                foreach (ZipArchiveEntry e in zip.Entries) total += e.Length;
                int last = -1;
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    string rel = e.FullName.Replace('/', '\\');
                    if (rel.StartsWith("iStream\\", StringComparison.OrdinalIgnoreCase)) rel = rel.Substring(8);
                    if (rel.Length == 0) continue;
                    string target = Path.GetFullPath(Path.Combine(dir, rel));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;   // never write outside the folder
                    if (rel.EndsWith("\\")) { Directory.CreateDirectory(target); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (Stream src = e.Open())
                    using (FileStream dst = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
                        src.CopyTo(dst, 1 << 16);
                    done += e.Length;
                    int pct = total == 0 ? 100 : (int)(done * 100 / total);
                    if (pct != last && progress != null) { last = pct; progress(pct); }
                }
            }

            using (Stream src = Payload.Open(Product.UninstallExe))
            using (FileStream dst = File.Create(Path.Combine(dir, Product.UninstallExe)))
                src.CopyTo(dst);

            if (progress != null) progress(-1);

            string exe = Path.Combine(dir, "iStream.exe");
            Shortcut(Product.StartMenuLink, exe, dir);
            if (desktop) Shortcut(Product.DesktopLink, exe, dir);

            // keep "Launch at Windows sign-in" pointing at the installed copy
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Product.RunKey, true))
                    if (k != null && k.GetValue("iStream") != null) k.SetValue("iStream", "\"" + exe + "\" --minimized");
            }
            catch { }

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Product.UninstallKey))
            {
                string un = Path.Combine(dir, Product.UninstallExe);
                k.SetValue("DisplayName", Product.Name);
                k.SetValue("DisplayVersion", Product.Version);
                k.SetValue("Publisher", Product.Publisher);
                k.SetValue("DisplayIcon", exe + ",0");
                k.SetValue("InstallLocation", dir);
                k.SetValue("UninstallString", "\"" + un + "\"");
                k.SetValue("QuietUninstallString", "\"" + un + "\" --quiet");
                k.SetValue("URLInfoAbout", Product.Url);
                k.SetValue("HelpLink", Product.Url);
                k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                k.SetValue("EstimatedSize", (int)(FolderSize(dir) / 1024), RegistryValueKind.DWord);
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        public static void Launch(string dir)
        {
            try
            {
                string exe = Path.Combine(Normalize(dir), "iStream.exe");
                Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true });
            }
            catch { }
        }

        static long FolderSize(string dir)
        {
            long n = 0;
            try { foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) n += new FileInfo(f).Length; } catch { }
            return n;
        }

        static void Shortcut(string link, string target, string workDir)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(link));
                dynamic sc = shell.CreateShortcut(link);
                sc.TargetPath = target;
                sc.WorkingDirectory = workDir;
                sc.IconLocation = target + ",0";
                sc.Description = "AirPlay screen mirroring for Windows";
                sc.Save();
                Marshal.FinalReleaseComObject(sc);
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }

        public static string LinkTarget(string link)
        {
            if (!File.Exists(link)) return null;
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                dynamic sc = shell.CreateShortcut(link);
                string target = sc.TargetPath;
                Marshal.FinalReleaseComObject(sc);
                return target;
            }
            catch { return null; }
            finally { Marshal.FinalReleaseComObject(shell); }
        }
    }

    static class Uninstaller
    {
        public static bool LooksInstalled(string dir)
        {
            return File.Exists(Path.Combine(dir, "iStream.exe")) && Directory.Exists(Path.Combine(dir, Product.EngineDir));
        }

        static bool Under(string path, string dir)
        {
            return path != null && path.StartsWith(dir + "\\", StringComparison.OrdinalIgnoreCase);
        }

        public static void Run(string dir, bool purge)
        {
            dir = Installer.Normalize(dir);
            Installer.KillRunning(dir + "\\");

            foreach (string link in new[] { Product.StartMenuLink, Product.DesktopLink })
                if (Under(Installer.LinkTarget(link), dir)) TryDelete(link);

            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Product.RunKey, true))
                {
                    string v = k == null ? null : k.GetValue("iStream") as string;
                    if (v != null && v.IndexOf(dir, StringComparison.OrdinalIgnoreCase) >= 0) k.DeleteValue("iStream", false);
                }
            }
            catch { }

            try
            {
                string registered = Product.InstalledDir();
                if (registered == null || string.Equals(Installer.Normalize(registered), dir, StringComparison.OrdinalIgnoreCase))
                    Registry.CurrentUser.DeleteSubKeyTree(Product.UninstallKey, false);
            }
            catch { }

            foreach (string f in Product.Files) TryDelete(Path.Combine(dir, f));
            try { Directory.Delete(Path.Combine(dir, Product.EngineDir), true); } catch { }

            if (purge) { try { Directory.Delete(Product.SettingsDir, true); } catch { } }

            // uninstall.exe can't delete itself while running: let a hidden cmd do it a moment after we exit,
            // then remove the folder only if it's empty.
            string self = Path.Combine(dir, Product.UninstallExe);
            string cmd = "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + self + "\" & rmdir \"" + dir + "\"";
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), cmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetTempPath()
                });
            }
            catch { }
        }

        static void TryDelete(string f) { try { if (File.Exists(f)) File.Delete(f); } catch { } }
    }

    // ------------------------------------------------------------------ window

    sealed class ProgressLine : Control
    {
        int value;
        public ProgressLine()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public int Value { get { return value; } set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Gfx.Prepare(e.Graphics, this);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Gfx.Fill(e.Graphics, r, r.Height / 2f, Theme.AccentTint);
            if (value > 0) Gfx.Fill(e.Graphics, new RectangleF(0, 0, Math.Max(r.Height, r.Width * value / 100f), r.Height), r.Height / 2f, Theme.Accent);
        }
    }

    sealed class SetupForm : Form
    {
        readonly bool installMode;
        readonly string uninstallDir;
        string dir;
        bool busy, finished;

        readonly PictureBox logo = new PictureBox();
        readonly Label title = new Label(), subtitle = new Label(), lblLocation = new Label(), lblPath = new Label(),
                       lblDesktop = new Label(), lblLaunch = new Label(), note = new Label(), status = new Label();
        readonly CardPanel pathBox = new CardPanel();
        readonly FlatButton btnChange = new FlatButton(BtnKind.Secondary), btnMain = new FlatButton(BtnKind.Primary),
                            btnCancel = new FlatButton(BtnKind.Secondary);
        readonly ToggleSwitch tgDesktop = new ToggleSwitch(), tgLaunch = new ToggleSwitch();
        readonly ProgressLine bar = new ProgressLine();

        static int D(float v) { return Gfx.D(v); }

        public SetupForm(bool installMode)
        {
            this.installMode = installMode;
            uninstallDir = Installer.Normalize(AppDomain.CurrentDomain.BaseDirectory);
            dir = Installer.Normalize(Product.InstalledDir() ?? Product.DefaultDir);

            Text = installMode ? "iStream Setup" : "Uninstall iStream";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(D(560), D(installMode ? 404 : 330));
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            logo.Image = IconArt.Draw(D(56), !Theme.IsDark);
            logo.SizeMode = PictureBoxSizeMode.AutoSize;
            logo.BackColor = Theme.Bg;

            Style(title, Theme.H1, Theme.Text);
            Style(subtitle, Theme.Small, Theme.Muted);
            Style(lblLocation, Theme.BodySemi, Theme.Text);
            Style(lblDesktop, Theme.Body, Theme.Text);
            Style(lblLaunch, Theme.Body, Theme.Text);
            Style(note, Theme.Small, Theme.Faint);
            Style(status, Theme.Small, Theme.Muted);
            lblPath.Font = Theme.Body; lblPath.ForeColor = Theme.Text; lblPath.BackColor = Theme.Field;
            lblPath.AutoEllipsis = true; lblPath.TextAlign = ContentAlignment.MiddleLeft;
            pathBox.SetColors(Theme.Field, Theme.Border);
            pathBox.Radius = 9f;
            pathBox.Controls.Add(lblPath);

            Controls.Add(logo);
            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(btnMain);
            Controls.Add(btnCancel);
            Controls.Add(bar);
            Controls.Add(status);
            bar.Visible = false;
            status.Visible = false;

            btnCancel.Text = T.S("cancel");
            btnCancel.Click += delegate { if (!busy) Close(); };
            btnMain.Click += delegate { OnMain(); };

            if (installMode) BuildInstall(); else BuildUninstall();
            AcceptButton = null;
        }

        static void Style(Label l, Font f, Color c)
        {
            l.Font = f; l.ForeColor = c; l.BackColor = Theme.Bg; l.AutoSize = false; l.UseMnemonic = false;
        }

        void BuildInstall()
        {
            title.Text = T.S("title");
            subtitle.Text = T.F("subtitle", Product.Version);
            lblLocation.Text = T.S("location");
            lblPath.Text = dir;
            btnChange.Text = T.S("change");
            btnChange.Click += delegate { PickFolder(); };
            lblDesktop.Text = T.S("desktop");
            lblLaunch.Text = T.S("launch");
            tgDesktop.SetSilently(true);
            tgLaunch.SetSilently(true);
            lblDesktop.Cursor = lblLaunch.Cursor = Cursors.Hand;
            lblDesktop.Click += delegate { if (!busy) tgDesktop.Checked = !tgDesktop.Checked; };
            lblLaunch.Click += delegate { if (!busy) tgLaunch.Checked = !tgLaunch.Checked; };
            note.Text = T.S("peruser");
            btnMain.Text = Product.InstalledDir() != null ? T.S("update") : T.S("install");

            foreach (Control c in new Control[] { lblLocation, pathBox, btnChange, tgDesktop, lblDesktop, tgLaunch, lblLaunch, note })
                Controls.Add(c);

            int x = D(32), w = ClientSize.Width - 2 * x;
            logo.Location = new Point(x, D(30));
            title.SetBounds(x + D(72), D(30), w - D(72), D(40));
            subtitle.SetBounds(x + D(74), D(70), w - D(74), D(22));

            lblLocation.SetBounds(x, D(118), w, D(24));
            int changeW = D(104), h = D(38);
            pathBox.SetBounds(x, D(146), w - changeW - D(10), h);
            lblPath.SetBounds(D(12), D(3), pathBox.Width - D(24), h - D(6));   // keep clear of the 1 px border
            btnChange.SetBounds(x + w - changeW, D(146), changeW, h);

            tgDesktop.SetBounds(x, D(208), D(40), D(22));
            lblDesktop.SetBounds(x + D(52), D(205), w - D(52), D(28));
            lblDesktop.TextAlign = ContentAlignment.MiddleLeft;
            tgLaunch.SetBounds(x, D(246), D(40), D(22));
            lblLaunch.SetBounds(x + D(52), D(243), w - D(52), D(28));
            lblLaunch.TextAlign = ContentAlignment.MiddleLeft;

            note.SetBounds(x, D(286), w, D(36));
            LayoutBottom();
        }

        void BuildUninstall()
        {
            title.Text = T.S("u_title");
            subtitle.Text = T.F("subtitle", Product.Version);
            lblLocation.Text = T.S("u_body");
            lblLocation.Font = Theme.Body;
            lblPath.Text = uninstallDir;
            lblLaunch.Text = T.S("u_settings");
            lblLaunch.Cursor = Cursors.Hand;
            lblLaunch.Click += delegate { if (!busy) tgLaunch.Checked = !tgLaunch.Checked; };
            btnMain.Text = T.S("u_button");

            foreach (Control c in new Control[] { lblLocation, pathBox, tgLaunch, lblLaunch })
                Controls.Add(c);

            int x = D(32), w = ClientSize.Width - 2 * x;
            logo.Location = new Point(x, D(30));
            title.SetBounds(x + D(72), D(30), w - D(72), D(40));
            subtitle.SetBounds(x + D(74), D(70), w - D(74), D(22));
            lblLocation.SetBounds(x, D(116), w, D(26));
            pathBox.SetBounds(x, D(146), w, D(38));
            lblPath.SetBounds(D(12), D(3), pathBox.Width - D(24), D(32));
            tgLaunch.SetBounds(x, D(206), D(40), D(22));
            lblLaunch.SetBounds(x + D(52), D(203), w - D(52), D(28));
            lblLaunch.TextAlign = ContentAlignment.MiddleLeft;
            LayoutBottom();

            if (!Uninstaller.LooksInstalled(uninstallDir))
            {
                lblLocation.Text = T.S("u_notfound");
                tgLaunch.Visible = lblLaunch.Visible = false;
                btnMain.Visible = false;
                btnCancel.Text = T.S("close");
            }
        }

        void LayoutBottom()
        {
            int x = D(32), w = ClientSize.Width - 2 * x, bh = D(40), y = ClientSize.Height - D(28) - bh;
            int mainW = Math.Max(D(132), TextRenderer.MeasureText(btnMain.Text, Theme.BodySemi).Width + D(40));
            btnMain.SetBounds(x + w - mainW, y, mainW, bh);
            btnCancel.SetBounds(btnMain.Left - D(10) - D(108), y, D(108), bh);
            status.SetBounds(x, y - D(34), w, D(22));
            bar.SetBounds(x, y - D(8), w, D(6));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.Dwm(Handle, 20, Theme.IsDark ? 1 : 0);
            Native.Dwm(Handle, 35, Theme.ColorRef(Theme.Bg));
            Native.Dwm(Handle, 34, Theme.ColorRef(Theme.Bg));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            btnMain.Focus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy) e.Cancel = true;
            base.OnFormClosing(e);
        }

        void PickFolder()
        {
            if (busy) return;
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = T.S("pick");
                dlg.ShowNewFolderButton = true;
                try { dlg.SelectedPath = Directory.Exists(dir) ? dir : Path.GetDirectoryName(dir); } catch { }
                if (dlg.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(dlg.SelectedPath)) return;
                string p = dlg.SelectedPath;
                if (!string.Equals(Path.GetFileName(p.TrimEnd('\\')), Product.Name, StringComparison.OrdinalIgnoreCase))
                    p = Path.Combine(p, Product.Name);
                dir = Installer.Normalize(p);
                lblPath.Text = dir;
            }
        }

        void SetBusy(bool on)
        {
            busy = on;
            btnMain.Enabled = btnCancel.Enabled = btnChange.Enabled = !on;
            tgDesktop.Enabled = tgLaunch.Enabled = !on;
            bar.Visible = status.Visible = on;
            Cursor = on ? Cursors.AppStarting : Cursors.Default;
        }

        void OnMain()
        {
            if (busy) return;
            if (finished)
            {
                if (installMode) Installer.Launch(dir);
                Close();
                return;
            }
            if (installMode) DoInstall(); else DoUninstall();
        }

        bool ConfirmKill(string onlyUnder)
        {
            if (!Installer.AnyRunning(onlyUnder)) return true;
            if (MessageBox.Show(this, T.S("running"), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return false;
            Installer.KillRunning(onlyUnder);
            return true;
        }

        void DoInstall()
        {
            if (!Installer.CanWrite(dir))
            {
                MessageBox.Show(this, T.F("noaccess", dir), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ConfirmKill(null)) return;

            bool desktop = tgDesktop.Checked, launch = tgLaunch.Checked;
            string target = dir;
            SetBusy(true);
            status.Text = T.F("copying", 0);
            bar.Value = 0;

            var th = new Thread(() =>
            {
                Exception error = null;
                try
                {
                    Installer.Install(target, desktop, pct => BeginInvoke(new Action(() =>
                    {
                        if (pct < 0) { status.Text = T.S("finishing"); bar.Value = 100; }
                        else { status.Text = T.F("copying", pct); bar.Value = pct; }
                    })));
                }
                catch (Exception ex) { error = ex; }

                BeginInvoke(new Action(() =>
                {
                    SetBusy(false);
                    if (error != null)
                    {
                        MessageBox.Show(this, T.F("failed", error.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (launch) { Installer.Launch(target); Close(); return; }
                    ShowFinished(T.S("done_t"), T.S("done_b"), T.S("open"));
                }));
            });
            th.IsBackground = true;
            th.SetApartmentState(ApartmentState.STA);   // WScript.Shell shortcuts
            th.Start();
        }

        void DoUninstall()
        {
            if (!ConfirmKill(uninstallDir + "\\")) return;
            bool purge = tgLaunch.Checked;
            SetBusy(true);
            status.Text = T.S("u_working");
            bar.Value = 30;
            var th = new Thread(() =>
            {
                Exception error = null;
                try { Uninstaller.Run(uninstallDir, purge); }
                catch (Exception ex) { error = ex; }
                BeginInvoke(new Action(() =>
                {
                    SetBusy(false);
                    if (error != null)
                    {
                        MessageBox.Show(this, T.F("failed", error.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    ShowFinished(T.S("u_done_t"), T.S("u_done_b"), T.S("close"));
                }));
            });
            th.IsBackground = true;
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        void ShowFinished(string head, string body, string button)
        {
            finished = true;
            title.Text = head;
            foreach (Control c in new Control[] { pathBox, btnChange, tgDesktop, lblDesktop, tgLaunch, lblLaunch, note, bar, status })
                c.Visible = false;
            lblLocation.Font = Theme.Body;
            lblLocation.ForeColor = Theme.Muted;
            lblLocation.Text = body;
            lblLocation.Height = D(60);
            btnMain.Text = button;
            btnCancel.Visible = installMode;
            btnCancel.Text = T.S("close");
            LayoutBottom();
            btnMain.Focus();
        }
    }
}
