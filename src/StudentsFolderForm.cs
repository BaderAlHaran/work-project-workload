using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Worksheets
{
    // Admin dialog: where student work is saved. Stored in the shared settings.ini, so it applies to every PC.
    class StudentsFolderForm : Form
    {
        readonly RadioButton desktop = new RadioButton();
        readonly RadioButton sync = new RadioButton();
        readonly RadioButton network = new RadioButton();
        readonly TextBox path = new TextBox();
        readonly NumericUpDown minutes = new NumericUpDown();
        readonly Label minutesLabel = new Label();

        public StudentsFolderForm()
        {
            Text = "مكان حفظ أعمال الطلاب";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(620), Ui.S(500));

            int x = Ui.S(20), y = Ui.S(16);
            var intro = new Label
            {
                Text = "عند اختيار الطالب لورقة عمل، تُنسخ إلى مجلد باسمه. اختر أين يكون هذا المجلد.\nينطبق هذا الإعداد على جميع الأجهزة من تسجيل الدخول القادم.",
                Location = new Point(x, y), Size = new Size(Ui.S(580), Ui.S(48)), ForeColor = Ui.Muted,
            };
            y += Ui.S(60);

            Radio(desktop, "سطح مكتب كل جهاز فقط", x, ref y);
            Hint("العمل يبقى على جهاز الطالب.", ref y);

            Radio(sync, "سطح المكتب + نسخ تلقائي إلى مجلد على الخادم  (موصى به)", x, ref y);
            Hint("الطالب يعمل على سطح المكتب، ويُنسخ عمله إلى الخادم تلقائياً دون حذف أي شيء هناك.", ref y);

            Radio(network, "الحفظ مباشرة في مجلد على الخادم", x, ref y);
            Hint("يتطلب أن يملك الطلاب صلاحية الكتابة في المجلد.", ref y);

            y += Ui.S(6);
            var pathLabel = Ui.Caption("مجلد الخادم:");
            pathLabel.Location = new Point(x, y);
            y += Ui.S(26);
            path.SetBounds(x, y, Ui.S(460), 0);
            path.RightToLeft = RightToLeft.No;
            path.Font = Ui.F(11f);
            var browse = Ui.Btn("استعراض…", false);
            browse.SetBounds(x + Ui.S(470), y - Ui.S(3), Ui.S(110), Ui.S(34));
            browse.Click += delegate { Browse(); };
            y += Ui.S(46);

            minutesLabel.Text = "النسخ التلقائي كل (دقيقة):";
            minutesLabel.AutoSize = true;
            minutesLabel.Location = new Point(x, y + Ui.S(3));
            minutes.Minimum = 1;
            minutes.Maximum = 240;
            minutes.Value = Storage.SyncMinutes;
            minutes.Font = Ui.F(11f);
            minutes.SetBounds(x + Ui.S(210), y, Ui.S(90), 0);
            y += Ui.S(40);

            var note = new Label
            {
                Text = "يُنسخ العمل أيضاً عند دخول الطالب وعند خروجه. الأعمال المحفوظة سابقاً لا تُنقل تلقائياً.",
                Location = new Point(x, y), Size = new Size(Ui.S(580), Ui.S(24)), ForeColor = Ui.Muted, Font = Ui.F(9f),
            };

            var ok = Ui.Btn("حفظ", true);
            ok.SetBounds(x, ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            ok.Click += delegate { Save(); };
            var cancel = Ui.Btn("إلغاء", false);
            cancel.SetBounds(x + Ui.S(140), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;

            Controls.AddRange(new Control[] { intro, pathLabel, path, browse, minutesLabel, minutes, note, ok, cancel });

            string mode = Storage.Mode;
            path.Text = mode == Storage.NetworkMode ? Storage.NetworkFolder : Storage.SyncFolder;
            if (path.Text.Length == 0) path.Text = mode == Storage.NetworkMode ? Storage.SyncFolder : Storage.NetworkFolder;
            (mode == Storage.SyncMode ? sync : mode == Storage.NetworkMode ? network : desktop).Checked = true;

            EventHandler refresh = delegate
            {
                path.Enabled = browse.Enabled = !desktop.Checked;
                minutes.Enabled = minutesLabel.Enabled = sync.Checked;
            };
            desktop.CheckedChanged += refresh;
            sync.CheckedChanged += refresh;
            network.CheckedChanged += refresh;
            refresh(null, null);
        }

        void Radio(RadioButton r, string text, int x, ref int y)
        {
            r.Text = text;
            r.Font = Ui.FB(10.5f);
            r.AutoSize = true;
            r.Location = new Point(x, y);
            Controls.Add(r);
            y += Ui.S(28);
        }

        void Hint(string text, ref int y)
        {
            Controls.Add(new Label
            {
                Text = text, ForeColor = Ui.Muted, Font = Ui.F(9f), AutoSize = true, Location = new Point(Ui.S(44), y),
            });
            y += Ui.S(32);
        }

        void Browse()
        {
            using (var d = new FolderBrowserDialog { Description = "اختر مجلد الخادم الذي تُجمع فيه أعمال الطلاب", ShowNewFolderButton = true })
            {
                if (Directory.Exists(path.Text)) d.SelectedPath = path.Text;
                if (d.ShowDialog(this) == DialogResult.OK) path.Text = d.SelectedPath;
            }
        }

        void Save()
        {
            string mode = sync.Checked ? Storage.SyncMode : network.Checked ? Storage.NetworkMode : Storage.DesktopMode;
            string folder = path.Text.Trim().TrimEnd('\\');
            if (mode != Storage.DesktopMode)
            {
                if (folder.Length == 0) { Ui.Error("الرجاء اختيار مجلد الخادم."); return; }
                if (!Path.IsPathRooted(folder)) { Ui.Error("الرجاء كتابة مسار كامل، مثل M:\\أعمال الطلاب"); return; }
                try
                {
                    // Checks from this (teacher's) PC only; student accounts may have fewer rights.
                    Directory.CreateDirectory(folder);
                    string probe = Path.Combine(folder, ".write-test-" + Environment.MachineName);
                    File.WriteAllText(probe, "");
                    File.Delete(probe);
                }
                catch (Exception ex)
                {
                    Ui.Error("لا يمكن الكتابة في هذا المجلد:\n" + ex.Message);
                    return;
                }
            }
            try { Storage.Save(mode, folder, folder, (int)minutes.Value); }
            catch (Exception ex) { Ui.Error("تعذر حفظ الإعداد:\n" + ex.Message); return; }

            string done =
                mode == Storage.DesktopMode ? "سيتم حفظ أعمال الطلاب على سطح مكتب كل جهاز." :
                mode == Storage.SyncMode ? "سيعمل الطلاب على سطح المكتب، ويُنسخ عملهم كل " + minutes.Value + " دقيقة إلى:\n" + folder :
                "سيتم حفظ أعمال الطلاب مباشرة في:\n" + folder;
            if (mode != Storage.DesktopMode) done += "\n\nتأكد أن حسابات الطلاب تملك صلاحية الإضافة في مجلد الخادم.";
            Ui.Info(done);
            DialogResult = DialogResult.OK;
        }
    }
}
