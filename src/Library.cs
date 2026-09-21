using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Worksheets
{
    class Grade
    {
        public int Number;
        public string Name;
        public bool HasTracks;
        public override string ToString() { return Name; }
    }

    class Student
    {
        public string Name;
        public Grade Grade;
        public int Section;
        public string Track; // null for grade 10

        public string ClassLabel
        {
            get
            {
                string s = Grade.Number + "/" + Section;
                return Track == null ? s : s + " - " + Track;
            }
        }

        // Folder names cannot contain "/", so 11/3 becomes 11-3.
        public string SectionFolder
        {
            get
            {
                string s = Grade.Number + "-" + Section;
                return Track == null ? s : s + " " + Track;
            }
        }

        public string Folder
        {
            get { return Path.Combine(AppPaths.Students, Grade.Name, SectionFolder, Catalog.SafeName(Name)); }
        }
    }

    class Worksheet
    {
        public string SourcePath;
        public int Number;
        public bool IsFolder { get { return Directory.Exists(SourcePath); } }
        public string OriginalName { get { return Path.GetFileName(SourcePath); } }

        public string Title
        {
            get
            {
                string n = IsFolder ? OriginalName : Path.GetFileNameWithoutExtension(SourcePath);
                if (n.StartsWith("ورقة عمل")) return n;
                return "ورقة عمل " + Number + " - " + n;
            }
        }

        public string DestinationFor(Student s) { return Path.Combine(s.Folder, Catalog.SafeName(Title)); }
    }

    static class AppPaths
    {
        public static readonly string Base = AppDomain.CurrentDomain.BaseDirectory;
        public static string Library { get { return Path.Combine(Base, "المكتبة"); } }
        public static string Settings { get { return Path.Combine(Base, "settings.ini"); } }

        static string students;
        public static string Students
        {
            get
            {
                if (students == null) students = ResolveStudents();
                return students;
            }
        }

        // On the Desktop so students and the teacher can find their work easily.
        static string ResolveStudents()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrEmpty(desktop)) desktop = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string p = Path.Combine(desktop, "ملفات الطلاب");
            Directory.CreateDirectory(p);
            return p;
        }

        static string loginLog;
        // Next to the exe (the shared M: folder) so the admin sees logins from every computer;
        // falls back to the student folder on the Desktop if the shared folder is read-only.
        public static string LoginLog
        {
            get
            {
                if (loginLog == null) loginLog = CanWrite(Base) ? Path.Combine(Base, "سجل الدخول.csv") : Path.Combine(Students, "سجل الدخول.csv");
                return loginLog;
            }
        }

        static bool CanWrite(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".write-test-" + Environment.MachineName);
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        public static bool IsNetwork(string path)
        {
            if (path.StartsWith(@"\\")) return true;
            try { return new DriveInfo(Path.GetPathRoot(path)).DriveType == DriveType.Network; }
            catch { return false; }
        }
    }

    static class Catalog
    {
        public static readonly Grade[] Grades =
        {
            new Grade { Number = 10, Name = "الصف العاشر" },
            new Grade { Number = 11, Name = "الصف الحادي عشر", HasTracks = true },
            new Grade { Number = 12, Name = "الصف الثاني عشر", HasTracks = true },
        };

        public static readonly string[] Tracks = { "علمي", "أدبي" };
        public const int SectionCount = 9;

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int StrCmpLogicalW(string a, string b);

        public static int NaturalCompare(string a, string b) { return StrCmpLogicalW(a, b); }

        public static string GradeFolder(Grade g) { return Path.Combine(AppPaths.Library, g.Name); }

        public static void EnsureStructure()
        {
            foreach (var g in Grades)
            {
                Directory.CreateDirectory(GradeFolder(g));
                if (g.HasTracks)
                    foreach (var t in Tracks) Directory.CreateDirectory(Path.Combine(GradeFolder(g), t));
            }
        }

        // Items directly inside the grade folder are shared by every track;
        // items inside the grade's علمي / أدبي subfolder are shown only to that track.
        public static List<Worksheet> WorksheetsFor(Student s)
        {
            var paths = new List<string>();
            string root = GradeFolder(s.Grade);
            AddChildren(paths, root, s.Grade.HasTracks);
            if (s.Track != null) AddChildren(paths, Path.Combine(root, s.Track), false);
            paths.Sort((a, b) => NaturalCompare(Path.GetFileName(a), Path.GetFileName(b)));

            var list = new List<Worksheet>();
            for (int i = 0; i < paths.Count; i++) list.Add(new Worksheet { SourcePath = paths[i], Number = i + 1 });
            return list;
        }

        static void AddChildren(List<string> list, string dir, bool skipTrackFolders)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var d in Directory.GetDirectories(dir))
            {
                if (skipTrackFolders && Tracks.Contains(Path.GetFileName(d))) continue;
                if (!IsHidden(d)) list.Add(d);
            }
            foreach (var f in Directory.GetFiles(dir))
                if (!IsHidden(f)) list.Add(f);
        }

        public static bool IsHidden(string p)
        {
            string n = Path.GetFileName(p);
            if (n.StartsWith(".") || n.StartsWith("~$")) return true;
            if (n.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase) || n.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase)) return true;
            try { return (File.GetAttributes(p) & (FileAttributes.Hidden | FileAttributes.System)) != 0; }
            catch { return false; }
        }

        public static string SafeName(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
                sb.Append(Path.GetInvalidFileNameChars().Contains(c) ? '-' : c);
            string r = sb.ToString().Trim().TrimEnd('.');
            return r.Length == 0 ? "بدون اسم" : r;
        }

        // Copies the worksheet into the student's folder. Existing work is never overwritten.
        public static bool Deliver(Worksheet w, Student s, out string dest)
        {
            dest = w.DestinationFor(s);
            if (Directory.Exists(dest)) return false;
            string tmp = dest + ".جاري-النسخ";
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            Directory.CreateDirectory(tmp);
            if (w.IsFolder) CopyDirectory(w.SourcePath, tmp);
            else File.Copy(w.SourcePath, Path.Combine(tmp, w.OriginalName));
            Directory.Move(tmp, dest);
            return true;
        }

        public static void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                string to = Path.Combine(dst, Path.GetFileName(f));
                File.Copy(f, to, true);
                File.SetAttributes(to, File.GetAttributes(to) & ~FileAttributes.ReadOnly);
            }
            foreach (var d in Directory.GetDirectories(src))
                CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        public static void Log(Student s, string action)
        {
            var now = DateTime.Now;
            string line = string.Join(",", new[]
            {
                now.ToString("yyyy-MM-dd"), now.ToString("HH:mm"),
                Csv(s.Name), Csv(s.Grade.Name), Csv(s.ClassLabel), Csv(action), Csv(Environment.MachineName)
            });
            // The log may live on the shared drive, so another computer can be writing at the same moment.
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    string path = AppPaths.LoginLog;
                    using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var w = new StreamWriter(fs, new UTF8Encoding(fs.Length == 0)))
                    {
                        if (fs.Length == 0) w.WriteLine("التاريخ,الوقت,الاسم,الصف,الشعبة,الإجراء,الجهاز");
                        w.WriteLine(line);
                    }
                    return;
                }
                catch (IOException) { System.Threading.Thread.Sleep(150); }
                catch { return; /* logging must never block a student */ }
            }
        }

        static string Csv(string v) { return "\"" + v.Replace("\"", "\"\"") + "\""; }
    }

    static class Shortcuts
    {
        public const string Name = "أوراق العمل";

        public static string DesktopPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Name + ".lnk"); }
        }

        public static string StartMenuPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Name + ".lnk"); }
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        class ShellLink { }

        // Unicode shell-link interface. WScript.Shell is not used because it saves through
        // an ANSI path and fails on Arabic file names such as "أوراق العمل.lnk".
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int cch, IntPtr fd, int flags);
            void GetIDList(out IntPtr pidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder dir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int showCmd);
            void SetShowCmd(int showCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int cch, out int icon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int icon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, int reserved);
            void Resolve(IntPtr hwnd, int flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
        }

        public static void Create(string lnkPath)
        {
            string exe = System.Windows.Forms.Application.ExecutablePath;
            var link = (IShellLinkW)new ShellLink();
            try
            {
                link.SetPath(exe);
                link.SetWorkingDirectory(Path.GetDirectoryName(exe));
                link.SetIconLocation(exe, 0);
                link.SetDescription("أوراق العمل - المرحلة الثانوية");
                Directory.CreateDirectory(Path.GetDirectoryName(lnkPath));
                ((System.Runtime.InteropServices.ComTypes.IPersistFile)link).Save(lnkPath, true);
            }
            finally { Marshal.FinalReleaseComObject(link); }
        }

        static string TargetOf(string lnkPath)
        {
            var link = (IShellLinkW)new ShellLink();
            try
            {
                ((System.Runtime.InteropServices.ComTypes.IPersistFile)link).Load(lnkPath, 0);
                var sb = new StringBuilder(1024);
                link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
                return sb.ToString();
            }
            finally { Marshal.FinalReleaseComObject(link); }
        }

        // Per computer (registry), not in the shared settings file on M:.
        static Microsoft.Win32.RegistryKey Key()
        {
            return Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\" + Name);
        }

        public static void CreateAll()
        {
            Create(DesktopPath);
            Create(StartMenuPath);
            using (var key = Key()) key.SetValue("ShortcutEnabled", 1);
        }

        // Runs at every start: once shortcuts are wanted on this computer, re-creates any that are
        // missing or point somewhere other than the exe that is running now (e.g. after moving to M:).
        public static void Sync()
        {
            try
            {
                string exe = System.Windows.Forms.Application.ExecutablePath;
                bool enabled, offered;
                using (var key = Key())
                {
                    enabled = key.GetValue("ShortcutEnabled") != null || File.Exists(DesktopPath) || File.Exists(StartMenuPath);
                    offered = key.GetValue("ShortcutOffered") != null;
                    key.SetValue("ShortcutOffered", 1);
                }

                if (enabled)
                {
                    foreach (var lnk in new[] { DesktopPath, StartMenuPath })
                    {
                        string target = null;
                        try { if (File.Exists(lnk)) target = TargetOf(lnk); } catch { }
                        if (!string.Equals(target, exe, StringComparison.OrdinalIgnoreCase)) Create(lnk);
                    }
                    using (var key = Key()) key.SetValue("ShortcutEnabled", 1);
                }
                else if (!offered && Ui.Confirm("هل تريد إنشاء اختصار للبرنامج على سطح المكتب لسهولة الدخول؟"))
                {
                    CreateAll();
                }
            }
            catch (Exception ex) { Ui.Error("تعذر تحديث الاختصار:\n" + ex.Message); }
        }
    }

    static class Settings
    {
        const string DefaultPassword = "1234";

        static Dictionary<string, string> Read()
        {
            var d = new Dictionary<string, string>();
            if (!File.Exists(AppPaths.Settings)) return d;
            foreach (var line in File.ReadAllLines(AppPaths.Settings, Encoding.UTF8))
            {
                int i = line.IndexOf('=');
                if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            return d;
        }

        static void Write(Dictionary<string, string> d)
        {
            File.WriteAllLines(AppPaths.Settings, d.Select(kv => kv.Key + "=" + kv.Value).ToArray(), Encoding.UTF8);
        }

        static string Hash(string s)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("ws-salt:" + s))).Replace("-", "");
        }

        public static bool CheckPassword(string p)
        {
            string h;
            if (!Read().TryGetValue("admin_hash", out h)) h = Hash(DefaultPassword);
            return h == Hash(p);
        }

        public static void SetPassword(string p)
        {
            var d = Read();
            d["admin_hash"] = Hash(p);
            Write(d);
        }
    }
}
