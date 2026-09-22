using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Worksheets
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        const string BaseArg = "--base";

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == BaseArg) AppPaths.Base = args[1];
            else if (RunLocalCopy()) return;

            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Catalog.EnsureStructure(); }
            catch (Exception ex) { Ui.Error("تعذر إنشاء مجلد المكتبة:\n" + ex.Message); }
            Shortcuts.Sync();

            // Login -> student/admin window -> back to login, until the login window is closed.
            while (true)
            {
                var login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK) break;
                Form next = login.AdminMode ? (Form)new AdminForm() : new StudentForm(login.Student);
                Application.Run(next);
                if (!(next.Tag is string && (string)next.Tag == "logout")) break;
            }
        }

        // When started from a network drive, runs a private copy of the exe from this PC instead,
        // passing the shared folder with --base. The exe on M: is then never locked, so the admin
        // can replace it at any time and every PC picks up the new version on its next start.
        // Each version is cached in its own folder, so a copy that is still running is never overwritten.
        static bool RunLocalCopy()
        {
            try
            {
                string exe = Application.ExecutablePath;
                string shared = Path.GetDirectoryName(exe);
                if (!AppPaths.IsNetwork(exe)) return false;

                var info = new FileInfo(exe);
                string version = info.LastWriteTimeUtc.Ticks.ToString("x") + "-" + info.Length.ToString("x");
                string cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Shortcuts.Name, "app");
                string local = Path.Combine(cacheRoot, version, Path.GetFileName(exe));

                if (!File.Exists(local))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(local));
                    File.Copy(exe, local + ".tmp", true);
                    File.Move(local + ".tmp", local);
                }

                // A trailing backslash before the closing quote would escape it, so none is passed.
                Process.Start(new ProcessStartInfo(local, BaseArg + " \"" + shared.TrimEnd('\\') + "\"")
                {
                    UseShellExecute = false,
                    WorkingDirectory = shared,
                });

                // Best-effort cleanup of older versions that are no longer running.
                foreach (var dir in Directory.GetDirectories(cacheRoot))
                {
                    if (dir.EndsWith(version)) continue;
                    try { Directory.Delete(dir, true); } catch { }
                }
                return true;
            }
            catch
            {
                return false; // fall back to running straight from the network
            }
        }
    }

    static class Ui
    {
        public static readonly Color Navy = Color.FromArgb(11, 42, 74);
        public static readonly Color NavyLight = Color.FromArgb(26, 69, 117);
        public static readonly Color Gold = Color.FromArgb(201, 162, 39);
        public static readonly Color Page = Color.FromArgb(244, 247, 251);
        public static readonly Color Border = Color.FromArgb(222, 229, 238);
        public static readonly Color Muted = Color.FromArgb(100, 110, 125);
        public static readonly Color Danger = Color.FromArgb(185, 45, 45);
        public static readonly Color Success = Color.FromArgb(22, 128, 70);

        public const string FontName = "Segoe UI";

        static float scale = -1;
        public static float Scale
        {
            get
            {
                if (scale < 0) using (var g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;
                return scale;
            }
        }

        public static int S(int px) { return (int)Math.Round(px * Scale); }
        public static Font F(float pt) { return new Font(FontName, pt); }
        public static Font FB(float pt) { return new Font(FontName, pt, FontStyle.Bold); }

        public static void Rtl(Form f)
        {
            f.RightToLeft = RightToLeft.Yes;
            f.RightToLeftLayout = true;
            f.Font = F(10.5f);
            f.BackColor = Page;
            f.AutoScaleMode = AutoScaleMode.None;
            try { f.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        }

        public static Button Btn(string text, bool primary)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = primary ? FB(10.5f) : F(10.5f),
                BackColor = primary ? Navy : Color.White,
                ForeColor = primary ? Color.White : Navy,
                Cursor = Cursors.Hand,
                Height = S(40),
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderColor = primary ? Navy : Border;
            b.FlatAppearance.MouseOverBackColor = primary ? NavyLight : Color.FromArgb(236, 242, 249);
            return b;
        }

        public static Panel Header(string title, string subtitle)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = S(subtitle == null ? 64 : 84), BackColor = Navy };
            var gold = new Panel { Dock = DockStyle.Bottom, Height = S(4), BackColor = Gold };
            var t = new Label
            {
                Text = title, ForeColor = Color.White, Font = FB(16f), AutoSize = true,
                Location = new Point(S(20), S(subtitle == null ? 14 : 10)), BackColor = Color.Transparent,
            };
            p.Controls.Add(gold);
            p.Controls.Add(t);
            if (subtitle != null)
            {
                var s = new Label
                {
                    Text = subtitle, ForeColor = Color.FromArgb(200, 215, 235), Font = F(10.5f), AutoSize = true,
                    Location = new Point(S(22), S(48)), BackColor = Color.Transparent, Name = "subtitle",
                };
                p.Controls.Add(s);
            }
            return p;
        }

        // Keeps Windows paths readable inside right-to-left text.
        public static string Ltr(string s) { return "‪" + s + "‬"; }

        public static Label Caption(string text)
        {
            return new Label { Text = text, AutoSize = true, Font = FB(10.5f), ForeColor = Navy };
        }

        public static void Error(string msg)
        {
            MessageBox.Show(msg, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }

        public static void Info(string msg)
        {
            MessageBox.Show(msg, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }

        public static bool Confirm(string msg)
        {
            return MessageBox.Show(msg, "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) == DialogResult.Yes;
        }

        public static void OpenInExplorer(string path, bool select)
        {
            try
            {
                if (select) System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path + "\"");
                else System.Diagnostics.Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex) { Error("تعذر فتح المجلد:\n" + ex.Message); }
        }

        // Simple one-line input dialog; returns null when cancelled.
        public static string Prompt(string title, string label, string initial, bool password)
        {
            var f = new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(S(420), S(160)), ShowInTaskbar = false,
            };
            Rtl(f);
            var l = new Label { Text = label, AutoSize = true, Location = new Point(S(20), S(18)) };
            var tb = new TextBox { Text = initial ?? "", Location = new Point(S(20), S(48)), Width = S(380), Font = F(11.5f) };
            if (password) tb.UseSystemPasswordChar = true;
            var ok = Btn("موافق", true);
            ok.SetBounds(S(20), S(100), S(120), S(40));
            ok.DialogResult = DialogResult.OK;
            var cancel = Btn("إلغاء", false);
            cancel.SetBounds(S(150), S(100), S(120), S(40));
            cancel.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange(new Control[] { l, tb, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }
    }
}
