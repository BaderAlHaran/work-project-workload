using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Worksheets
{
    // Lets a student open one form of a Visual Basic project on its own, or the whole project.
    class FormPicker : Form
    {
        public Editors.VbForm Chosen { get; private set; } // null = the whole project

        public FormPicker(string project, List<Editors.VbForm> forms, string startup)
        {
            Text = "اختر النموذج";
            Ui.Rtl(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            var list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(Ui.S(20), Ui.S(16), Ui.S(20), Ui.S(16)), AutoScroll = true,
            };
            list.Controls.Add(new Label
            {
                Text = "«" + project + "» فيه " + forms.Count + " نماذج. أي نموذج تريد أن تفتح؟",
                Font = Ui.FB(11.5f), ForeColor = Ui.Navy, AutoSize = true, Margin = new Padding(0, 0, 0, Ui.S(4)),
            });
            list.Controls.Add(new Label
            {
                Text = "يُفتح النموذج في Visual Studio، وعند الضغط على ▶ (F5) يعمل هذا النموذج وحده.",
                Font = Ui.F(9.5f), ForeColor = Ui.Muted, AutoSize = true, Margin = new Padding(0, 0, 0, Ui.S(12)),
            });

            foreach (var f in forms)
            {
                var form = f;
                list.Controls.Add(Choice(form.ToString(), true, delegate { Chosen = form; DialogResult = DialogResult.OK; }));
            }
            list.Controls.Add(Choice("المشروع كاملاً" + (startup != null ? "  (يبدأ من " + startup + ")" : ""), false,
                delegate { Chosen = null; DialogResult = DialogResult.OK; }));

            Controls.Add(list);
            ClientSize = new Size(Ui.S(460), Ui.S(130) + (forms.Count + 1) * Ui.S(50));
        }

        static Button Choice(string text, bool primary, System.EventHandler click)
        {
            var b = Ui.Btn(text, primary);
            b.Width = Ui.S(410);
            b.Height = Ui.S(42);
            b.TextAlign = ContentAlignment.MiddleRight;
            b.Margin = new Padding(0, 0, 0, Ui.S(8));
            b.Click += click;
            return b;
        }
    }
}
