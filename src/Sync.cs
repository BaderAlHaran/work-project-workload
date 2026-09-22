using System;
using System.IO;
using System.Threading;

namespace Worksheets
{
    // The admin's choice of where student work goes, stored in the shared settings.ini.
    static class Storage
    {
        public const string DesktopMode = "desktop", NetworkMode = "network", SyncMode = "sync";
        public const int DefaultSyncMinutes = 30;

        public static string Mode
        {
            get
            {
                string m = Settings.Get("students_mode");
                if (!string.IsNullOrEmpty(m)) return m;
                // Settings saved before modes existed: a folder meant "network".
                return string.IsNullOrEmpty(Settings.Get("students_folder")) ? DesktopMode : NetworkMode;
            }
        }

        public static string NetworkFolder { get { return Settings.Get("students_folder") ?? ""; } }
        public static string SyncFolder { get { return Settings.Get("sync_folder") ?? ""; } }

        public static int SyncMinutes
        {
            get
            {
                int m;
                return int.TryParse(Settings.Get("sync_minutes"), out m) && m >= 1 ? m : DefaultSyncMinutes;
            }
        }

        // Where the teacher finds the collected work.
        public static string TeacherFolder
        {
            get { return Mode == SyncMode && SyncFolder.Length > 0 ? SyncFolder : AppPaths.Students; }
        }

        public static void Save(string mode, string networkFolder, string syncFolder, int syncMinutes)
        {
            Settings.Set("students_mode", mode);
            Settings.Set("students_folder", mode == NetworkMode ? networkFolder : "");
            if (mode == SyncMode)
            {
                Settings.Set("sync_folder", syncFolder);
                Settings.Set("sync_minutes", syncMinutes.ToString());
            }
        }
    }

    // Copies a student's Desktop work to the server folder. Only adds and updates files;
    // never deletes anything on the server, so a student can't wipe what was already collected.
    class WorkSync
    {
        readonly string source, dest;
        readonly object gate = new object();

        public DateTime? LastSuccess { get; private set; }
        public string LastError { get; private set; }
        public event Action Finished; // raised on the thread that ran the copy

        WorkSync(string source, string dest)
        {
            this.source = source;
            this.dest = dest;
        }

        public string Destination { get { return dest; } }

        // Null unless the admin chose "Desktop + automatic copy".
        public static WorkSync For(Student s)
        {
            if (Storage.Mode != Storage.SyncMode || Storage.SyncFolder.Length == 0) return null;
            return new WorkSync(s.Folder, Path.Combine(Storage.SyncFolder, s.RelativeFolder));
        }

        // Background copy for the timer; skipped if a copy is already running.
        public void RunInBackground()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (!Monitor.TryEnter(gate)) return;
                try { CopyNow(); }
                finally { Monitor.Exit(gate); }
                Raise();
            });
        }

        // Blocking copy for logout: waits for a running copy, then does a final pass.
        public bool RunNow()
        {
            bool ok;
            lock (gate) ok = CopyNow();
            Raise();
            return ok;
        }

        void Raise()
        {
            var h = Finished;
            if (h != null) h();
        }

        bool CopyNow()
        {
            int failed = 0;
            string firstError = null;
            try
            {
                if (Directory.Exists(source)) CopyNewer(source, dest, ref failed, ref firstError);
            }
            catch (Exception ex)
            {
                failed++;
                if (firstError == null) firstError = ex.Message;
            }

            if (failed == 0)
            {
                LastSuccess = DateTime.Now;
                LastError = null;
                return true;
            }
            LastError = firstError;
            return false;
        }

        static void CopyNewer(string src, string dst, ref int failed, ref string firstError)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(f);
                if (name.StartsWith(".write-test")) continue;
                string to = Path.Combine(dst, name);
                try
                {
                    var s = new FileInfo(f);
                    var d = new FileInfo(to);
                    if (d.Exists && d.Length == s.Length && d.LastWriteTimeUtc >= s.LastWriteTimeUtc) continue;
                    File.Copy(f, to, true);
                }
                catch (Exception ex)
                {
                    // e.g. a file the editor has open; it is retried on the next pass.
                    failed++;
                    if (firstError == null) firstError = ex.Message;
                }
            }
            foreach (var d in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(d);
                if (name == "__pycache__" || name.EndsWith(".جاري-النسخ")) continue;
                CopyNewer(d, Path.Combine(dst, name), ref failed, ref firstError);
            }
        }
    }
}
