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
        readonly RadioButton network = new RadioButton();
        readonly TextBox path = new TextBox();

        public StudentsFolderForm()
        {
            Text = "مكان حفظ أعمال الطلاب";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(580), Ui.S(380));

            var intro = new Label
            {
                Text = "عند اختيار الطالب لورقة عمل، تُنسخ إلى مجلد باسمه داخل المكان التالي.\nينطبق هذا الإعداد على جميع الأجهزة من تسجيل الدخول القادم.",
                Location = new Point(Ui.S(20), Ui.S(16)), Size = new Size(Ui.S(540), Ui.S(48)), ForeColor = Ui.Muted,
            };

            desktop.Text = "سطح مكتب كل جهاز  (" + AppPaths.StudentsFolderName + " على سطح المكتب)";
            desktop.Location = new Point(Ui.S(20), Ui.S(76));
            desktop.AutoSize = true;

            network.Text = "مجلد مشترك على الشبكة (مثل M:\\أعمال الطلاب) — يجمع أعمال كل الطلاب في مكان واحد:";
            network.Location = new Point(Ui.S(20), Ui.S(116));
            network.AutoSize = true;

            path.SetBounds(Ui.S(44), Ui.S(156), Ui.S(400), 0);
            path.RightToLeft = RightToLeft.No;
            path.Font = Ui.F(11f);
            path.TextChanged += delegate { if (path.Text.Length > 0) network.Checked = true; };

            var browse = Ui.Btn("استعراض…", false);
            browse.SetBounds(Ui.S(452), Ui.S(152), Ui.S(108), Ui.S(34));
            browse.Click += delegate { Browse(); };

            var note = new Label
            {
                Text = "• يجب أن تملك أجهزة الطلاب صلاحية الكتابة في هذا المجلد.\n" +
                       "• إذا تعذر الوصول إليه يوماً، يُحفظ العمل مؤقتاً على سطح المكتب.\n" +
                       "• الأعمال المحفوظة سابقاً لا تُنقل تلقائياً.",
                Location = new Point(Ui.S(20), Ui.S(204)), Size = new Size(Ui.S(540), Ui.S(70)), ForeColor = Ui.Muted, Font = Ui.F(9.5f),
            };

            var ok = Ui.Btn("حفظ", true);
            ok.SetBounds(Ui.S(20), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            ok.Click += delegate { Save(); };
            var cancel = Ui.Btn("إلغاء", false);
            cancel.SetBounds(Ui.S(160), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            CancelButton = cancel;

            Controls.AddRange(new Control[] { intro, desktop, network, path, browse, note, ok, cancel });

            string current = Settings.Get("students_folder");
            path.Text = current ?? "";
            if (string.IsNullOrEmpty(current)) desktop.Checked = true; else network.Checked = true;
        }

        void Browse()
        {
            using (var d = new FolderBrowserDialog { Description = "اختر المجلد الذي تُحفظ فيه أعمال الطلاب", ShowNewFolderButton = true })
            {
                if (Directory.Exists(path.Text)) d.SelectedPath = path.Text;
                if (d.ShowDialog(this) == DialogResult.OK) path.Text = d.SelectedPath;
            }
        }

        void Save()
        {
            string value = "";
            if (network.Checked)
            {
                value = path.Text.Trim().TrimEnd('\\');
                if (value.Length == 0) { Ui.Error("الرجاء اختيار المجلد."); return; }
                if (!Path.IsPathRooted(value)) { Ui.Error("الرجاء كتابة مسار كامل، مثل M:\\أعمال الطلاب"); return; }
                try
                {
                    // Make sure the folder exists and is writable from this PC before saving.
                    Directory.CreateDirectory(value);
                    string probe = Path.Combine(value, ".write-test-" + Environment.MachineName);
                    File.WriteAllText(probe, "");
                    File.Delete(probe);
                }
                catch (Exception ex)
                {
                    Ui.Error("لا يمكن الكتابة في هذا المجلد:\n" + ex.Message);
                    return;
                }
            }
            try { Settings.Set("students_folder", value); }
            catch (Exception ex) { Ui.Error("تعذر حفظ الإعداد:\n" + ex.Message); return; }
            Ui.Info(value.Length == 0
                ? "سيتم حفظ أعمال الطلاب على سطح مكتب كل جهاز."
                : "سيتم حفظ أعمال الطلاب في:\n" + value);
            DialogResult = DialogResult.OK;
        }
    }
}
