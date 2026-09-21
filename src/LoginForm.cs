using System;
using System.Drawing;
using System.Windows.Forms;

namespace Worksheets
{
    class LoginForm : Form
    {
        public Student Student { get; private set; }
        public bool AdminMode { get; private set; }

        readonly TextBox name = new TextBox();
        readonly ComboBox grade = new ComboBox();
        readonly ComboBox section = new ComboBox();
        readonly ComboBox track = new ComboBox();
        readonly Label trackLabel;

        public LoginForm()
        {
            Text = "أوراق العمل - تسجيل الدخول";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.S(480), Ui.S(560));

            var header = Ui.Header("أوراق العمل", "المرحلة الثانوية - الصف العاشر إلى الثاني عشر");
            Controls.Add(header);

            int x = Ui.S(40), w = Ui.S(400), y = header.Height + Ui.S(28);

            Controls.Add(At(Ui.Caption("اسم الطالب"), x, y));
            y += Ui.S(28);
            name.Font = Ui.F(13f);
            name.SetBounds(x, y, w, 0);
            name.MaxLength = 60;
            Controls.Add(name);
            y += name.Height + Ui.S(20);

            Controls.Add(At(Ui.Caption("الصف"), x, y));
            y += Ui.S(28);
            Combo(grade, x, y, w);
            foreach (var g in Catalog.Grades) grade.Items.Add(g);
            y += grade.Height + Ui.S(20);

            Controls.Add(At(Ui.Caption("الشعبة"), x, y));
            trackLabel = Ui.Caption("التخصص");
            int half = (w - Ui.S(16)) / 2;
            Controls.Add(At(trackLabel, x + half + Ui.S(16), y));
            y += Ui.S(28);
            Combo(section, x, y, half);
            Combo(track, x + half + Ui.S(16), y, half);
            track.Items.AddRange(Catalog.Tracks);
            y += section.Height + Ui.S(32);

            var login = Ui.Btn("دخول", true);
            login.Font = Ui.FB(13f);
            login.SetBounds(x, y, w, Ui.S(50));
            login.Click += delegate { DoLogin(); };
            Controls.Add(login);
            AcceptButton = login;

            var admin = new LinkLabel
            {
                Text = "دخول المسؤول", AutoSize = true, LinkColor = Ui.Muted, ActiveLinkColor = Ui.Navy,
                LinkBehavior = LinkBehavior.HoverUnderline, Font = Ui.F(9.5f),
            };
            admin.Location = new Point(x, ClientSize.Height - Ui.S(36));
            admin.LinkClicked += delegate { DoAdmin(); };
            Controls.Add(admin);

            grade.SelectedIndexChanged += delegate { OnGradeChanged(); };
            grade.SelectedIndex = 0;
        }

        static Control At(Control c, int x, int y) { c.Location = new Point(x, y); return c; }

        void Combo(ComboBox c, int x, int y, int w)
        {
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Font = Ui.F(12f);
            c.SetBounds(x, y, w, 0);
            Controls.Add(c);
        }

        void OnGradeChanged()
        {
            var g = (Grade)grade.SelectedItem;
            int keep = section.SelectedIndex;
            section.Items.Clear();
            for (int i = 1; i <= Catalog.SectionCount; i++) section.Items.Add(g.Number + "/" + i);
            section.SelectedIndex = keep >= 0 ? keep : -1;
            track.Visible = trackLabel.Visible = g.HasTracks;
        }

        void DoLogin()
        {
            string n = name.Text.Trim();
            while (n.Contains("  ")) n = n.Replace("  ", " ");
            if (n.Length < 3) { Ui.Info("الرجاء كتابة اسم الطالب."); name.Focus(); return; }
            var g = (Grade)grade.SelectedItem;
            if (section.SelectedIndex < 0) { Ui.Info("الرجاء اختيار الشعبة."); section.Focus(); return; }
            if (g.HasTracks && track.SelectedIndex < 0) { Ui.Info("الرجاء اختيار التخصص (علمي أو أدبي)."); track.Focus(); return; }

            Student = new Student
            {
                Name = n,
                Grade = g,
                Section = section.SelectedIndex + 1,
                Track = g.HasTracks ? (string)track.SelectedItem : null,
            };
            DialogResult = DialogResult.OK;
        }

        void DoAdmin()
        {
            string p = Ui.Prompt("دخول المسؤول", "كلمة المرور:", "", true);
            if (p == null) return;
            if (!Settings.CheckPassword(p)) { Ui.Error("كلمة المرور غير صحيحة."); return; }
            AdminMode = true;
            DialogResult = DialogResult.OK;
        }
    }
}
