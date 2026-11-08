// Update check and self-update.
// Asks GitHub's public API for the latest release, downloads its installer, verifies the SHA-256
// (GitHub's asset digest, or the checksum line in the release notes) and runs it quietly.
// No personal data is sent: just an HTTPS GET with a "iStream/<version>" user agent.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace IStream
{
    sealed class UpdateInfo
    {
        public Version Version;
        public string Tag, PageUrl, AssetName, AssetUrl, Sha256;
        public string VersionText { get { return Updater.Text(Version); } }
    }

    static class Updater
    {
        const string LatestApi = "https://api.github.com/repos/olehnykorovych/iStream-AirPlay/releases/latest";
        public const string ReleasesPage = "https://github.com/olehnykorovych/iStream-AirPlay/releases/latest";
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\iStream";

        // For testing against a local file: ISTREAM_UPDATE_URL=file:///C:/path/latest.json
        static string ApiUrl
        {
            get
            {
                string o = Environment.GetEnvironmentVariable("ISTREAM_UPDATE_URL");
                return string.IsNullOrEmpty(o) ? LatestApi : o;
            }
        }

        public static Version Current
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            }
        }

        public static string Text(Version v)
        {
            return v.Build > 0 ? v.ToString(3) : v.ToString(2) + ".0";
        }

        static WebRequest Request(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            WebRequest r = WebRequest.Create(url);
            r.Timeout = 20000;
            var h = r as HttpWebRequest;
            if (h != null)
            {
                h.UserAgent = "iStream/" + Text(Current);
                h.Accept = url.Contains("api.github.com") ? "application/vnd.github+json" : "*/*";
                h.ReadWriteTimeout = 30000;
            }
            return r;
        }

        // Returns the latest release if it's newer than this copy, otherwise null. Throws on network errors.
        public static UpdateInfo Check()
        {
            string json;
            using (WebResponse resp = Request(ApiUrl).GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                json = sr.ReadToEnd();

            var ser = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
            var root = ser.Deserialize<Dictionary<string, object>>(json);
            if (Get(root, "draft") is bool && (bool)Get(root, "draft")) return null;
            if (Get(root, "prerelease") is bool && (bool)Get(root, "prerelease")) return null;

            string tag = Get(root, "tag_name") as string;
            Version v = ParseTag(tag);
            if (v == null || v <= Current) return null;

            var u = new UpdateInfo { Version = v, Tag = tag, PageUrl = (Get(root, "html_url") as string) ?? ReleasesPage };
            var assets = Get(root, "assets") as ArrayList;
            if (assets != null)
                foreach (object o in assets)
                {
                    var a = o as Dictionary<string, object>;
                    string name = a == null ? null : Get(a, "name") as string;
                    if (name == null || !Regex.IsMatch(name, @"^iStream-Setup-[\d.]+\.exe$", RegexOptions.IgnoreCase)) continue;
                    u.AssetName = name;
                    u.AssetUrl = Get(a, "browser_download_url") as string;
                    string digest = Get(a, "digest") as string;   // "sha256:<hex>"
                    if (digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) u.Sha256 = digest.Substring(7);
                }
            if (u.AssetName != null && u.Sha256 == null)
            {
                // fallback: "<64 hex>  iStream-Setup-x.y.z.exe" in the release notes
                string body = Get(root, "body") as string ?? "";
                Match m = Regex.Match(body, @"\b([0-9A-Fa-f]{64})\s+" + Regex.Escape(u.AssetName));
                if (m.Success) u.Sha256 = m.Groups[1].Value;
            }
            return u;
        }

        static object Get(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) ? v : null; }

        static Version ParseTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            Match m = Regex.Match(tag, @"^v?(\d+)\.(\d+)(?:\.(\d+))?$");
            if (!m.Success) return null;
            return new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0);
        }

        // An update can install itself only when this copy was installed with the installer (not the portable ZIP).
        public static bool CanSelfUpdate(UpdateInfo u)
        {
            return u != null && u.AssetUrl != null && u.Sha256 != null && IsInstalledCopy;
        }

        public static bool IsInstalledCopy
        {
            get
            {
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(UninstallKey))
                    {
                        string dir = k == null ? null : k.GetValue("InstallLocation") as string;
                        if (string.IsNullOrEmpty(dir)) return false;
                        string here = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\');
                        return string.Equals(Path.GetFullPath(dir).TrimEnd('\\'), here, StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch { return false; }
            }
        }

        // Downloads the installer to %TEMP%\iStream-update and checks its SHA-256. Returns the file path.
        public static string Download(UpdateInfo u, Action<int> progress)
        {
            string dir = Path.Combine(Path.GetTempPath(), "iStream-update");
            Directory.CreateDirectory(dir);
            foreach (string old in Directory.GetFiles(dir, "iStream-Setup-*.exe"))
                try { File.Delete(old); } catch { }
            string path = Path.Combine(dir, Path.GetFileName(u.AssetName));
            string part = path + ".part";

            using (WebResponse resp = Request(u.AssetUrl).GetResponse())
            using (Stream src = resp.GetResponseStream())
            using (FileStream dst = File.Create(part))
            using (var sha = SHA256.Create())
            {
                long total = resp.ContentLength, done = 0;
                var buf = new byte[1 << 16];
                int n, last = -1;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, n);
                    sha.TransformBlock(buf, 0, n, null, 0);
                    done += n;
                    int pct = total > 0 ? (int)(done * 100 / total) : -1;
                    if (pct != last && progress != null) { last = pct; progress(pct); }
                }
                sha.TransformFinalBlock(buf, 0, 0);
                string got = BitConverter.ToString(sha.Hash).Replace("-", "");
                if (!string.Equals(got, u.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    dst.Close();
                    File.Delete(part);
                    throw new InvalidDataException("Checksum mismatch for " + u.AssetName);
                }
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(part, path);
            return path;
        }

        // Runs the installer quietly. It waits for this process to exit, installs over this copy and starts
        // iStream again (hidden in the tray when it was hidden).
        public static void RunInstaller(string installer, bool background)
        {
            string dir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\');
            string args = "--quiet --update --wait-pid " + Process.GetCurrentProcess().Id +
                          " --dir \"" + dir + "\"" + (background ? " --background" : "");
            Process.Start(new ProcessStartInfo(installer, args) { UseShellExecute = false, WorkingDirectory = Path.GetTempPath() });
        }
    }
}
