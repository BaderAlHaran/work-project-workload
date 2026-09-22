using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Worksheets
{
    // How a worksheet is opened after it is copied into the student's folder.
    // The choice is stored in the shared settings.ini so it applies to every PC.
    static class Editors
    {
        public const string Auto = "auto", PyCharm = "pycharm", VSCode = "vscode", Default = "default", Explorer = "explorer", Custom = "custom";

        public static string Mode
        {
            get { return Settings.Get("open_mode") ?? Auto; }
            set { Settings.Set("open_mode", value); }
        }

        public static string CustomPath
        {
            get { return Settings.Get("open_custom") ?? ""; }
            set { Settings.Set("open_custom", value); }
        }

        // The folder always opens in Explorer so students can see their files; the editor opens alongside it.
        public static void Open(string folder)
        {
            Ui.OpenInExplorer(folder, false);
            try { TryOpen(folder); }
            catch { }
        }

        static bool TryOpen(string folder)
        {
            string main = MainFile(folder);
            switch (Mode)
            {
                case Explorer: return false;
                case PyCharm: return LaunchIde(FindPyCharm(), folder, main);
                case VSCode: return LaunchIde(FindVSCode(), folder, main);
                case Default: return OpenWithDefault(main);
                case Custom: return LaunchIde(File.Exists(CustomPath) ? CustomPath : null, folder, main);
                default:
                    return LaunchIde(FindPyCharm(), folder, main)
                        || LaunchIde(FindVSCode(), folder, main)
                        || OpenWithDefault(main);
            }
        }

        // IDEs get the worksheet folder as the project, plus its main file so it is already open.
        static bool LaunchIde(string exe, string folder, string main)
        {
            if (exe == null) return false;
            string args = Quote(folder) + (main != null ? " " + Quote(main) : "");
            Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true, WorkingDirectory = folder });
            return true;
        }

        // Prefers the "Edit" verb so a .py file opens in an editor (IDLE) instead of running.
        static bool OpenWithDefault(string file)
        {
            if (file == null) return false;
            var psi = new ProcessStartInfo(file) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(file) };
            if (psi.Verbs.Contains("edit", StringComparer.OrdinalIgnoreCase)) psi.Verb = "edit";
            else if (Path.GetExtension(file).Equals(".py", StringComparison.OrdinalIgnoreCase)) return false; // would just run it
            Process.Start(psi);
            return true;
        }

        static string Quote(string s) { return "\"" + s.TrimEnd('\\') + "\""; }

        static readonly string[] Priority = { ".py", ".ipynb", ".sln", ".vbproj", ".html", ".htm", ".docx", ".doc", ".pdf", ".pptx", ".xlsx", ".txt", ".rtf" };
        static readonly string[] SkipDirs = { "bin", "obj", "venv", "__pycache__", "node_modules" };

        // The file students most likely want to start with: best type first, then the shallowest.
        public static string MainFile(string folder)
        {
            var found = new List<Tuple<int, int, string>>();
            Collect(folder, 0, found);
            return found.OrderBy(t => t.Item1).ThenBy(t => t.Item2)
                        .ThenBy(t => Path.GetFileName(t.Item3), Comparer<string>.Create(Catalog.NaturalCompare))
                        .Select(t => t.Item3).FirstOrDefault();
        }

        static void Collect(string dir, int depth, List<Tuple<int, int, string>> found)
        {
            if (depth > 4) return;
            try
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith(".") || name.StartsWith("__init__")) continue;
                    int rank = Array.IndexOf(Priority, Path.GetExtension(f).ToLowerInvariant());
                    if (rank >= 0) found.Add(Tuple.Create(rank, depth, f));
                }
                foreach (var d in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(d);
                    if (name.StartsWith(".") || SkipDirs.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    Collect(d, depth + 1, found);
                }
            }
            catch { }
        }

        // ---------- detection ----------

        public static string FindPyCharm()
        {
            string p = AppPath("pycharm64.exe") ?? AppPath("pycharm.exe");
            if (p != null) return p;

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "JetBrains"),
                Path.Combine(local, "Programs"),
            };
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                var hit = SafeDirs(root).Where(d => Path.GetFileName(d).StartsWith("PyCharm", StringComparison.OrdinalIgnoreCase))
                                        .OrderByDescending(d => d, Comparer<string>.Create(Catalog.NaturalCompare))
                                        .Select(d => Path.Combine(d, "bin", "pycharm64.exe"))
                                        .FirstOrDefault(File.Exists);
                if (hit != null) return hit;
            }

            // JetBrains Toolbox installs
            string toolbox = Path.Combine(local, "JetBrains", "Toolbox");
            string script = Path.Combine(toolbox, "scripts", "pycharm.cmd");
            if (File.Exists(script)) return script;
            string apps = Path.Combine(toolbox, "apps");
            if (Directory.Exists(apps))
            {
                try
                {
                    var exe = Directory.GetFiles(apps, "pycharm64.exe", SearchOption.AllDirectories)
                                       .OrderByDescending(f => f, Comparer<string>.Create(Catalog.NaturalCompare)).FirstOrDefault();
                    if (exe != null) return exe;
                }
                catch { }
            }
            return null;
        }

        public static string FindVSCode()
        {
            string p = AppPath("code.exe");
            if (p != null) return p;
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (var c in new[]
            {
                Path.Combine(local, "Programs", "Microsoft VS Code", "Code.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe"),
            })
                if (File.Exists(c)) return c;
            return null;
        }

        static string AppPath(string exe)
        {
            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                try
                {
                    using (var k = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe))
                    {
                        string v = k == null ? null : k.GetValue("") as string;
                        if (!string.IsNullOrEmpty(v)) { v = v.Trim('"'); if (File.Exists(v)) return v; }
                    }
                }
                catch { }
            }
            return null;
        }

        static string[] SafeDirs(string root)
        {
            try { return Directory.GetDirectories(root); }
            catch { return new string[0]; }
        }
    }
}
