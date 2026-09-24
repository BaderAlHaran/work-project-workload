using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Worksheets
{
    // Admin dialog: which program opens a worksheet, chosen per grade.
    class OpenModeForm : Form
    {
        static readonly string[] Modes =
            { Editors.Auto, Editors.PyCharm, Editors.VSCode, Editors.VisualBasic, Editors.Default, Editors.Explorer, Editors.Custom };
        static readonly string[] Labels =
        {
            "تلقائي: PyCharm ثم VS Code (ومشاريع VB في Visual Basic)",
            "PyCharm",
            "Visual Studio Code",
            "Visual Basic (أي إصدار من Visual Studio)",
            "البرنامج الافتراضي للملف",
            "المجلد فقط (بدون برنامج)",
            "برنامج آخر (المحدد بالأسفل)",
        };

        readonly ComboBox[] combos = new ComboBox[Catalog.Grades.Length];
        readonly TextBox custom = new TextBox();

        public OpenModeForm()
        {
            Text = "طريقة فتح التمارين";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Ui.S(620), Ui.S(470));

            int x = Ui.S(20), y = Ui.S(16);
            Controls.Add(new Label
            {
                Text = "عند اختيار الطالب لتمرين، يُنسخ إلى مجلده ويُفتح المجلد، ثم يُفتح بالبرنامج التالي.\nينطبق هذا الإعداد على جميع الأجهزة.",
                Location = new Point(x, y), Size = new Size(Ui.S(580), Ui.S(48)), ForeColor = Ui.Muted,
            });
            y += Ui.S(62);

            for (int i = 0; i < Catalog.Grades.Length; i++)
            {
                var g = Catalog.Grades[i];
                var label = Ui.Caption(g.Name + ":");
                label.Location = new Point(x, y + Ui.S(4));
                var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Ui.F(10.5f) };
                c.Items.AddRange(Labels);
                c.SetBounds(x + Ui.S(150), y, Ui.S(430), 0);
                int current = Array.IndexOf(Modes, Editors.ModeFor(g));
                c.SelectedIndex = current < 0 ? 0 : current;
                combos[i] = c;
                Controls.Add(label);
                Controls.Add(c);
                y += Ui.S(46);
            }

            y += Ui.S(4);
            var customLabel = Ui.Caption("البرنامج الآخر:");
            customLabel.Location = new Point(x, y + Ui.S(4));
            custom.SetBounds(x + Ui.S(150), y, Ui.S(312), 0);
            custom.Text = Editors.CustomPath;
            custom.RightToLeft = RightToLeft.No;
            var browse = Ui.Btn("استعراض…", false);
            browse.SetBounds(x + Ui.S(470), y - Ui.S(3), Ui.S(110), Ui.S(34));
            browse.Click += delegate { Browse(); };
            Controls.AddRange(new Control[] { customLabel, custom, browse });
            y += Ui.S(52);

            // What this PC has, so the admin can see if the choice will work here.
            Controls.Add(new Label
            {
                Text = "على هذا الجهاز:   PyCharm " + Mark(Editors.FindPyCharm() != null) +
                       "     VS Code " + Mark(Editors.FindVSCode() != null) +
                       "     Visual Basic " + Mark(Editors.HasVisualBasic()) +
                       "\nالبرنامج المختار يجب أن يكون مثبتاً على أجهزة الطلاب، وإلا يُفتح المجلد فقط.",
                Location = new Point(x, y), Size = new Size(Ui.S(580), Ui.S(52)), ForeColor = Ui.Muted, Font = Ui.F(9.5f),
            });

            var ok = Ui.Btn("حفظ", true);
            ok.SetBounds(x, ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            ok.Click += delegate { Save(); };
            var cancel = Ui.Btn("إلغاء", false);
            cancel.SetBounds(x + Ui.S(140), ClientSize.Height - Ui.S(56), Ui.S(130), Ui.S(40));
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(ok);
            Controls.Add(cancel);
            CancelButton = cancel;
        }

        static string Mark(bool found) { return found ? "✔" : "✘"; }

        void Browse()
        {
            using (var d = new OpenFileDialog { Filter = "البرامج (*.exe;*.cmd;*.bat)|*.exe;*.cmd;*.bat", Title = "اختر البرنامج" })
                if (d.ShowDialog(this) == DialogResult.OK) custom.Text = d.FileName;
        }

        void Save()
        {
            bool usesCustom = combos.Any(c => Modes[c.SelectedIndex] == Editors.Custom);
            if (usesCustom && !File.Exists(custom.Text.Trim()))
            {
                Ui.Error("الرجاء اختيار ملف «البرنامج الآخر».");
                return;
            }
            try
            {
                for (int i = 0; i < combos.Length; i++) Editors.SetModeFor(Catalog.Grades[i], Modes[combos[i].SelectedIndex]);
                if (usesCustom) Editors.CustomPath = custom.Text.Trim();
            }
            catch (Exception ex) { Ui.Error("تعذر حفظ الإعداد:\n" + ex.Message); return; }
            DialogResult = DialogResult.OK;
        }
    }
}
