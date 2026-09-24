using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Worksheets
{
    // How a worksheet is opened after it is copied into the student's folder.
    // Chosen per grade by the admin and stored in the shared settings.ini, so it applies to every PC.
    static class Editors
    {
        public const string Auto = "auto", PyCharm = "pycharm", VSCode = "vscode", VisualBasic = "vb",
                            Default = "default", Explorer = "explorer", Custom = "custom";

        // Grade 12 works on Visual Basic projects; grades 10 and 11 on Python.
        public static string ModeFor(Grade g)
        {
            string m = Settings.Get("open_mode_" + g.Number);
            if (!string.IsNullOrEmpty(m)) return m;
            if (g.Number == 12) return VisualBasic;
            return Settings.Get("open_mode") ?? Auto; // setting saved before it was per grade
        }

        public static void SetModeFor(Grade g, string mode) { Settings.Set("open_mode_" + g.Number, mode); }

        public static string CustomPath
        {
            get { return Settings.Get("open_custom") ?? ""; }
            set { Settings.Set("open_custom", value); }
        }

        // True when this worksheet will open in Visual Basic, so the student can first pick a form.
        public static bool OpensInVisualBasic(string folder, Grade grade)
        {
            string mode = ModeFor(grade);
            return (mode == VisualBasic || mode == Auto) && VisualBasicProject(folder) != null;
        }

        // The folder always opens in Explorer so students can see their files; the editor opens alongside it.
        // vbForm: a form of a VB project to open on its own, or null for the whole project.
        public static void Open(string folder, Grade grade, VbForm vbForm)
        {
            Ui.OpenInExplorer(folder, false);
            try { TryOpen(folder, ModeFor(grade), vbForm); }
            catch { }
        }

        static bool TryOpen(string folder, string mode, VbForm vbForm)
        {
            string main = MainFile(folder);
            string vbProject = VisualBasicProject(folder);
            switch (mode)
            {
                case Explorer: return false;
                case PyCharm: return LaunchIde(FindPyCharm(), folder, main);
                case VSCode: return LaunchIde(FindVSCode(), folder, main);
                case Default: return OpenWithDefault(main);
                case Custom: return LaunchIde(File.Exists(CustomPath) ? CustomPath : null, folder, main);
                case VisualBasic:
                    // Worksheets without a VB project (pictures, web pages) open in their normal program,
                    // but never in a Python editor.
                    if (vbProject != null) return OpenVisualBasic(vbProject, vbForm);
                    return main != null && !IsPython(main) && OpenWithDefault(main);
                default:
                    if (vbProject != null && OpenVisualBasic(vbProject, vbForm)) return true;
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
            else if (IsPython(file)) return false; // would just run it
            Process.Start(psi);
            return true;
        }

        static string Quote(string s) { return "\"" + s.TrimEnd('\\') + "\""; }
        static bool IsPython(string file) { return Path.GetExtension(file).Equals(".py", StringComparison.OrdinalIgnoreCase); }

        // ---------- choosing the file to open ----------

        static readonly string[] Priority = { ".py", ".ipynb", ".sln", ".vbproj", ".vbp", ".html", ".htm", ".docx", ".doc", ".pdf", ".pptx", ".xlsx", ".txt", ".rtf", ".png", ".jpg", ".gif", ".mdb" };
        static readonly string[] VbTypes = { ".sln", ".vbproj", ".vbp" };
        static readonly string[] SkipDirs = { "bin", "obj", "venv", "__pycache__", "node_modules" };

        public static bool IsVisualBasic(string ext) { return VbTypes.Contains(ext.ToLowerInvariant()); }

        // The file students most likely want to start with: best type first, then the shallowest.
        public static string MainFile(string folder) { return Best(folder, Priority); }

        static string VisualBasicProject(string folder) { return Best(folder, VbTypes); }

        static string Best(string folder, string[] types)
        {
            var found = new List<Tuple<int, int, string>>();
            Collect(folder, 0, types, found);
            return found.OrderBy(t => t.Item1).ThenBy(t => t.Item2)
                        .ThenBy(t => Path.GetFileName(t.Item3), Comparer<string>.Create(Catalog.NaturalCompare))
                        .Select(t => t.Item3).FirstOrDefault();
        }

        static void Collect(string dir, int depth, string[] types, List<Tuple<int, int, string>> found)
        {
            if (depth > 4) return;
            try
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith(".") || name.StartsWith("__init__")) continue;
                    int rank = Array.IndexOf(types, Path.GetExtension(f).ToLowerInvariant());
                    if (rank >= 0) found.Add(Tuple.Create(rank, depth, f));
                }
                foreach (var d in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(d);
                    if (name.StartsWith(".") || SkipDirs.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    Collect(d, depth + 1, types, found);
                }
            }
            catch { }
        }

        // ---------- Visual Basic (any version) ----------

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern uint AssocQueryString(uint flags, uint str, string assoc, string extra, [Out] StringBuilder output, ref uint length);
        const uint AssocStrExecutable = 2;

        // The program Windows opens this file type with, or null when there is none.
        static string AssociatedProgram(string ext)
        {
            var sb = new StringBuilder(1024);
            uint len = (uint)sb.Capacity;
            if (AssocQueryString(0, AssocStrExecutable, ext, "open", sb, ref len) != 0) return null;
            string exe = sb.ToString();
            if (!File.Exists(exe) || Path.GetFileName(exe).Equals("OpenWith.exe", StringComparison.OrdinalIgnoreCase)) return null;
            return exe;
        }

        static bool OpenVisualBasic(string project, VbForm vbForm)
        {
            string ext = Path.GetExtension(project).ToLowerInvariant();
            string dir = Path.GetDirectoryName(project);

            // A single form: start Visual Studio itself so it can be told to open that form's file
            // (the association below can only open the whole solution).
            if (vbForm != null && ext != ".vbp")
            {
                string devenv = FindVisualStudio();
                if (devenv != null)
                {
                    string args = Quote(project) + " /Command \"File.OpenFile " + Path.GetFileName(vbForm.File) + "\"";
                    Process.Start(new ProcessStartInfo(devenv, args) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(vbForm.File) });
                    return true;
                }
            }

            // 1) The file association. For .sln this is Visual Studio's Version Selector,
            //    which picks the right installed version for the project by itself.
            if (AssociatedProgram(ext) != null)
            {
                Process.Start(new ProcessStartInfo(project) { UseShellExecute = true, WorkingDirectory = dir });
                return true;
            }

            // 2) Search for an installed Visual Studio / Visual Basic.
            string exe = ext == ".vbp" ? FindVB6() : FindVisualStudio();
            if (exe == null) return false;
            Process.Start(new ProcessStartInfo(exe, Quote(project)) { UseShellExecute = true, WorkingDirectory = dir });
            return true;
        }

        // Newest installed Visual Studio or Visual Basic Express, any version from 2005 on.
        public static string FindVisualStudio()
        {
            string newest = VsWhere();
            if (newest != null) return newest;

            foreach (var exe in new[] { "devenv.exe", "vbexpress.exe", "wdexpress.exe", "VSWinExpress.exe" })
            {
                string p = AppPath(exe);
                if (p != null) return p;
            }

            // Registry entries of VS 2005-2015 and the Express editions, newest version first.
            var hits = new List<Tuple<double, string>>();
            foreach (var product in new[] { "VisualStudio", "VBExpress", "WDExpress", "VSWinExpress" })
                foreach (var root in new[] { @"SOFTWARE\Microsoft\", @"SOFTWARE\WOW6432Node\Microsoft\" })
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(root + product))
                        {
                            if (key == null) continue;
                            foreach (var ver in key.GetSubKeyNames())
                            {
                                double v;
                                if (!double.TryParse(ver, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) continue;
                                using (var vk = key.OpenSubKey(ver))
                                {
                                    string dir = vk == null ? null : vk.GetValue("InstallDir") as string;
                                    if (string.IsNullOrEmpty(dir)) continue;
                                    foreach (var exe in new[] { "devenv.exe", "vbexpress.exe", "WDExpress.exe", "VSWinExpress.exe" })
                                    {
                                        string p = Path.Combine(dir, exe);
                                        if (File.Exists(p)) hits.Add(Tuple.Create(v, p));
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            return hits.OrderByDescending(h => h.Item1).Select(h => h.Item2).FirstOrDefault();
        }

        // Visual Studio 2017 and newer register themselves with vswhere instead of the registry.
        static string VsWhere()
        {
            string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                          "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (!File.Exists(vswhere)) return null;
            try
            {
                var psi = new ProcessStartInfo(vswhere, "-latest -products * -property productPath")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    string first = output.Split('\n').Select(l => l.Trim()).FirstOrDefault(File.Exists);
                    return first;
                }
            }
            catch { return null; }
        }

        public static string FindVB6()
        {
            string p = AppPath("VB6.EXE");
            if (p != null) return p;
            foreach (var pf in new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles })
            {
                string c = Path.Combine(Environment.GetFolderPath(pf), "Microsoft Visual Studio", "VB98", "VB6.EXE");
                if (File.Exists(c)) return c;
            }
            return null;
        }

        // ---------- the forms inside a VB project ----------

        public class VbForm
        {
            public string File;       // Form2.vb
            public string ClassName;  // Form2
            public string Caption;    // السؤال الأول (the form's title bar text)
            public override string ToString() { return Caption.Length > 0 ? ClassName + " — " + Caption : ClassName; }
        }

        public static string VisualBasicProjectFile(string folder) { return Best(folder, new[] { ".vbproj" }); }

        // Windows Forms in the project: X.vb with an X.Designer.vb that inherits Form.
        public static List<VbForm> VisualBasicForms(string folder)
        {
            var forms = new List<VbForm>();
            string proj = VisualBasicProjectFile(folder);
            if (proj == null) return forms;
            foreach (var designer in Directory.GetFiles(Path.GetDirectoryName(proj), "*.Designer.vb"))
            {
                string code = designer.Substring(0, designer.Length - ".Designer.vb".Length) + ".vb";
                if (!File.Exists(code)) continue;
                string text = ReadText(designer);
                if (!Regex.IsMatch(text, @"Inherits\s+System\.Windows\.Forms\.Form\b")) continue;
                var cls = Regex.Match(text, @"Partial\s+(?:Public\s+|Friend\s+)?Class\s+(\w+)");
                var caption = Regex.Match(text, @"Me\.Text\s*=\s*""([^""]*)""");
                forms.Add(new VbForm
                {
                    File = code,
                    ClassName = cls.Success ? cls.Groups[1].Value : Path.GetFileNameWithoutExtension(code),
                    Caption = caption.Success ? caption.Groups[1].Value : "",
                });
            }
            forms.Sort((a, b) => Catalog.NaturalCompare(a.ClassName, b.ClassName));
            return forms;
        }

        // The form the project starts with, per My Project\Application.myapp.
        public static string StartupForm(string folder)
        {
            string proj = VisualBasicProjectFile(folder);
            if (proj == null) return null;
            string myapp = Path.Combine(Path.GetDirectoryName(proj), "My Project", "Application.myapp");
            if (!File.Exists(myapp)) return null;
            var m = Regex.Match(ReadText(myapp), @"<MainForm>\s*(\w+)\s*</MainForm>");
            return m.Success ? m.Groups[1].Value : null;
        }

        // Makes F5 run the chosen form. Only ever called on the student's own copy.
        // VB keeps the startup form in two places: Application.myapp and Application.Designer.vb.
        public static void SetStartupForm(string folder, string className)
        {
            string proj = VisualBasicProjectFile(folder);
            if (proj == null || string.IsNullOrEmpty(className)) return;
            string dir = Path.Combine(Path.GetDirectoryName(proj), "My Project");
            Rewrite(Path.Combine(dir, "Application.myapp"), @"(<MainForm>)\s*\w+\s*(</MainForm>)", "${1}" + className + "${2}");
            Rewrite(Path.Combine(dir, "Application.Designer.vb"), @"(Me\.MainForm\s*=\s*Global\.[\w.]*?\.)\w+(\s*$)", "${1}" + className + "${2}");
        }

        static void Rewrite(string file, string pattern, string replacement)
        {
            if (!File.Exists(file)) return;
            Encoding enc;
            string text;
            using (var r = new StreamReader(file, Encoding.UTF8, true)) { text = r.ReadToEnd(); enc = r.CurrentEncoding; }
            string updated = Regex.Replace(text, pattern, replacement, RegexOptions.Multiline);
            if (updated != text) File.WriteAllText(file, updated, enc);
        }

        static string ReadText(string file)
        {
            using (var r = new StreamReader(file, Encoding.UTF8, true)) return r.ReadToEnd();
        }

        public static bool HasVisualBasic()
        {
            return AssociatedProgram(".sln") != null || FindVisualStudio() != null || FindVB6() != null;
        }

        // ---------- Python editors ----------

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
