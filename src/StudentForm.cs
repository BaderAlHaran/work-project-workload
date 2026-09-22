using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Worksheets
{
    class StudentForm : Form
    {
        readonly Student student;
        readonly FlowLayoutPanel cards = new FlowLayoutPanel();
        readonly Label empty = new Label();

        public StudentForm(Student s)
        {
            student = s;
            Text = "أوراق العمل - " + s.Name;
            Ui.Rtl(this);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.S(960), Ui.S(640));
            MinimumSize = new Size(Ui.S(620), Ui.S(420));

            var header = Ui.Header("مرحباً، " + s.Name, s.Grade.Name + "  |  الشعبة " + s.ClassLabel);
            header.Width = ClientSize.Width;

            var logout = Ui.Btn("تسجيل الخروج", false);
            logout.Width = Ui.S(140);
            logout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logout.Location = new Point(ClientSize.Width - logout.Width - Ui.S(20), Ui.S(20));
            logout.Click += delegate { Tag = "logout"; Close(); };
            header.Controls.Add(logout);

            var mine = Ui.Btn("📂 مجلدي", false);
            mine.Width = Ui.S(140);
            mine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            mine.Location = new Point(logout.Left - mine.Width - Ui.S(10), Ui.S(20));
            mine.Click += delegate { Directory.CreateDirectory(student.Folder); Ui.OpenInExplorer(student.Folder, false); };
            header.Controls.Add(mine);

            var bar = new Panel { Dock = DockStyle.Top, Height = Ui.S(56), Padding = new Padding(Ui.S(24), 0, Ui.S(24), 0) };
            var title = new Label
            {
                Text = "اختر ورقة العمل لفتحها — سيتم حفظ نسخة منها في مجلدك الخاص",
                Font = Ui.FB(12f), ForeColor = Ui.Navy, AutoSize = true, Location = new Point(Ui.S(24), Ui.S(18)),
            };
            bar.Controls.Add(title);

            var status = new Label
            {
                Dock = DockStyle.Bottom, Height = Ui.S(30), ForeColor = Ui.Muted, Font = Ui.F(9f),
                Text = "  مجلدك: " + Ui.Ltr(student.Folder), TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.White, AutoEllipsis = true,
            };

            cards.Dock = DockStyle.Fill;
            cards.AutoScroll = true;
            cards.Padding = new Padding(Ui.S(18), Ui.S(4), Ui.S(18), Ui.S(18));

            empty.Text = "لا توجد أوراق عمل لهذا الصف حالياً.\nيمكن للمسؤول إضافتها من «دخول المسؤول».";
            empty.Font = Ui.F(12f);
            empty.ForeColor = Ui.Muted;
            empty.TextAlign = ContentAlignment.MiddleCenter;
            empty.Dock = DockStyle.Fill;
            empty.Visible = false;

            Controls.Add(cards);
            Controls.Add(empty);
            Controls.Add(status);
            Controls.Add(bar);
            Controls.Add(header);

            Catalog.Log(student, "دخول");
            try { Directory.CreateDirectory(student.Folder); }
            catch (Exception ex) { Ui.Error("تعذر إنشاء مجلد الطالب:\n" + ex.Message); }
            Activated += delegate { Reload(); };

            // Picks up changes the admin makes on the shared drive while this window stays open.
            var poll = new Timer { Interval = 20000 };
            poll.Tick += delegate { if (Signature(SafeList()) != shown) Reload(); };
            poll.Start();
            FormClosed += delegate { poll.Dispose(); };
        }

        string shown;

        List<Worksheet> SafeList()
        {
            try { return Catalog.WorksheetsFor(student); }
            catch { return new List<Worksheet>(); } // shared drive briefly unreachable
        }

        static string Signature(List<Worksheet> list)
        {
            return string.Join("|", list.Select(w => w.SourcePath).ToArray());
        }

        void Reload()
        {
            var list = SafeList();
            shown = Signature(list);
            cards.SuspendLayout();
            cards.Controls.Clear();
            foreach (var w in list) cards.Controls.Add(Card(w));
            empty.Visible = list.Count == 0;
            cards.Visible = list.Count > 0;
            cards.ResumeLayout();
        }

        Control Card(Worksheet w)
        {
            bool done = Directory.Exists(w.DestinationFor(student));
            var card = new Panel
            {
                Size = new Size(Ui.S(280), Ui.S(138)), BackColor = Color.White, Cursor = Cursors.Hand,
                Margin = new Padding(Ui.S(8)),
            };
            card.Paint += (o, e) =>
            {
                using (var pen = new Pen(Ui.Border)) e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using (var br = new SolidBrush(done ? Ui.Success : Ui.Gold))
                    e.Graphics.FillRectangle(br, card.Width - Ui.S(5), 0, Ui.S(5), card.Height);
            };

            var num = new Label
            {
                Text = "ورقة عمل " + w.Number, Font = Ui.FB(9.5f), ForeColor = Ui.Gold, AutoSize = true,
                Location = new Point(Ui.S(16), Ui.S(12)),
            };
            var name = new Label
            {
                Text = w.IsFolder ? w.OriginalName : Path.GetFileNameWithoutExtension(w.OriginalName),
                Font = Ui.FB(11.5f), ForeColor = Ui.Navy, AutoEllipsis = true,
                Location = new Point(Ui.S(16), Ui.S(34)), Size = new Size(Ui.S(250), Ui.S(50)),
            };
            var meta = new Label
            {
                Text = Describe(w), Font = Ui.F(9f), ForeColor = Ui.Muted, AutoSize = true,
                Location = new Point(Ui.S(16), Ui.S(88)),
            };
            var state = new Label
            {
                Text = done ? "✔ محفوظة في مجلدك" : "جديدة", Font = Ui.FB(9f),
                ForeColor = done ? Ui.Success : Ui.Muted, AutoSize = true, Location = new Point(Ui.S(16), Ui.S(110)),
            };
            card.Controls.AddRange(new Control[] { num, name, meta, state });

            var tip = new ToolTip();
            tip.SetToolTip(card, w.OriginalName);
            EventHandler open = delegate { Open(w); };
            card.Click += open;
            foreach (Control c in card.Controls) { c.Click += open; c.Cursor = Cursors.Hand; tip.SetToolTip(c, w.OriginalName); }
            return card;
        }

        static string Describe(Worksheet w)
        {
            if (!w.IsFolder)
            {
                string ext = Path.GetExtension(w.SourcePath).TrimStart('.').ToUpperInvariant();
                return "ملف " + ext + "  •  " + HumanSize(new FileInfo(w.SourcePath).Length);
            }
            long bytes = 0;
            int count = 0;
            try
            {
                foreach (var f in Directory.GetFiles(w.SourcePath, "*", SearchOption.AllDirectories))
                {
                    count++;
                    bytes += new FileInfo(f).Length;
                }
            }
            catch { }
            return "مجلد  •  " + count + " ملف  •  " + HumanSize(bytes);
        }

        static string HumanSize(long b)
        {
            if (b < 1024) return b + " بايت";
            if (b < 1024 * 1024) return (b / 1024.0).ToString("0.#") + " كيلوبايت";
            return (b / 1024.0 / 1024.0).ToString("0.#") + " ميجابايت";
        }

        void Open(Worksheet w)
        {
            string dest;
            try
            {
                Cursor = Cursors.WaitCursor;
                if (Catalog.Deliver(w, student, out dest)) Catalog.Log(student, "فتح " + w.Title);
            }
            catch (Exception ex)
            {
                Ui.Error("تعذر نسخ ورقة العمل إلى مجلدك:\n" + ex.Message);
                return;
            }
            finally { Cursor = Cursors.Default; }
            Cursor = Cursors.WaitCursor;
            Editors.Open(dest);
            Cursor = Cursors.Default;
            Reload();
        }
    }
}
