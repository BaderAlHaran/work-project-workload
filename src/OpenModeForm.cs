using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Worksheets
{
    // Admin dialog: choose which program opens a worksheet after a student picks it.
    class OpenModeForm : Form
    {
        readonly RadioButton[] radios;
        readonly string[] modes = { Editors.Auto, Editors.PyCharm, Editors.VSCode, Editors.Default, Editors.Explorer, Editors.Custom };
        readonly TextBox custom = new TextBox();

        public OpenModeForm()
        {
            Text = "طريقة فتح أوراق العمل";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(560), Ui.S(470));

            string py = Editors.FindPyCharm(), code = Editors.FindVSCode();
            string[] labels =
            {
                "تلقائي (موصى به): PyCharm، ثم VS Code، ثم البرنامج الافتراضي",
                "PyCharm  —  " + Found(py),
                "Visual Studio Code  —  " + Found(code),
                "البرنامج الافتراضي للملف (ملفات بايثون تفتح في IDLE)",
                "مستكشف الملفات فقط (فتح المجلد)",
                "برنامج آخر:",
            };

            var intro = new Label
            {
                Text = "عند اختيار الطالب لورقة عمل، تُنسخ إلى مجلده ثم تُفتح بالطريقة التالية.\nينطبق هذا الإعداد على جميع الأجهزة.",
                Location = new Point(Ui.S(20), Ui.S(16)), Size = new Size(Ui.S(520), Ui.S(48)), ForeColor = Ui.Muted,
            };
            Controls.Add(intro);

            radios = new RadioButton[modes.Length];
            int y = Ui.S(72);
            for (int i = 0; i < modes.Length; i++)
            {
                radios[i] = new RadioButton { Text = labels[i], Location = new Point(Ui.S(20), y), AutoSize = true };
                Controls.Add(radios[i]);
                y += Ui.S(40);
            }

            custom.SetBounds(Ui.S(44), y, Ui.S(380), 0);
            custom.Text = Editors.CustomPath;
            custom.RightToLeft = RightToLeft.No;
            Controls.Add(custom);
            var browse = Ui.Btn("استعراض…", false);
            browse.SetBounds(Ui.S(432), y - Ui.S(4), Ui.S(108), Ui.S(34));
            browse.Click += delegate { Browse(); };
            Controls.Add(browse);
            y += Ui.S(52);

            var note = new Label
            {
                Text = "ملاحظة: البرنامج المختار يجب أن يكون مثبتاً على أجهزة الطلاب. إذا لم يوجد، يُفتح المجلد في المستكشف.",
                Location = new Point(Ui.S(20), y), Size = new Size(Ui.S(520), Ui.S(40)), ForeColor = Ui.Muted, Font = Ui.F(9f),
            };
            Controls.Add(note);

            var ok = Ui.Btn("حفظ", true);
            ok.SetBounds(Ui.S(20), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            ok.Click += delegate { Save(); };
            var cancel = Ui.Btn("إلغاء", false);
            cancel.SetBounds(Ui.S(160), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(ok);
            Controls.Add(cancel);
            CancelButton = cancel;

            int current = Array.IndexOf(modes, Editors.Mode);
            radios[current < 0 ? 0 : current].Checked = true;
            custom.TextChanged += delegate { if (custom.Text.Length > 0) radios[modes.Length - 1].Checked = true; };
        }

        static string Found(string path) { return path != null ? "مثبت على هذا الجهاز" : "غير مثبت على هذا الجهاز"; }

        void Browse()
        {
            using (var d = new OpenFileDialog { Filter = "البرامج (*.exe;*.cmd;*.bat)|*.exe;*.cmd;*.bat", Title = "اختر البرنامج" })
                if (d.ShowDialog(this) == DialogResult.OK) custom.Text = d.FileName;
        }

        void Save()
        {
            string mode = modes[Array.FindIndex(radios, r => r.Checked)];
            if (mode == Editors.Custom && !File.Exists(custom.Text.Trim()))
            {
                Ui.Error("الرجاء اختيار ملف البرنامج.");
                return;
            }
            try
            {
                Editors.Mode = mode;
                if (mode == Editors.Custom) Editors.CustomPath = custom.Text.Trim();
            }
            catch (Exception ex) { Ui.Error("تعذر حفظ الإعداد:\n" + ex.Message); return; }
            DialogResult = DialogResult.OK;
        }
    }
}
