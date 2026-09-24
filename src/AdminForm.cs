using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace Worksheets
{
    class AdminForm : Form
    {
        readonly TreeView tree = new TreeView();
        readonly Label status = new Label();
        const string Placeholder = "…";

        public AdminForm()
        {
            Text = "أوراق العمل - لوحة المسؤول";
            Ui.Rtl(this);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Ui.S(1040), Ui.S(740));
            MinimumSize = new Size(Ui.S(760), Ui.S(500));

            var header = Ui.Header("لوحة المسؤول", "إدارة أوراق العمل: إضافة وحذف وتعديل الملفات");
            header.Width = ClientSize.Width;
            var logout = Ui.Btn("خروج", false);
            logout.Width = Ui.S(120);
            logout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logout.Location = new Point(ClientSize.Width - logout.Width - Ui.S(20), Ui.S(22));
            logout.Click += delegate { Tag = "logout"; Close(); };
            header.Controls.Add(logout);

            var side = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, Width = Ui.S(280), FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(Ui.S(12)), BackColor = Color.White, AutoScroll = true,
            };
            side.Controls.Add(Section("الملفات"));
            side.Controls.Add(ActionButton("➕ إضافة ملفات", AddFiles, true));
            side.Controls.Add(ActionButton("📁 إضافة مجلد", AddFolder, true));
            side.Controls.Add(ActionButton("🆕 تمرين جديد (مجلد فارغ)", NewFolder, false));
            side.Controls.Add(ActionButton("✏️ إعادة تسمية", Rename, false));
            side.Controls.Add(ActionButton("🗑️ حذف", Delete, false));
            side.Controls.Add(ActionButton("🔍 فتح في المستكشف", ShowInExplorer, false));
            side.Controls.Add(ActionButton("🔄 تحديث", delegate { LoadTree(); }, false));
            side.Controls.Add(Section("الطلاب"));
            side.Controls.Add(ActionButton("👥 ملفات الطلاب", delegate { Ui.OpenInExplorer(Storage.TeacherFolder, false); }, false));
            side.Controls.Add(ActionButton("📂 مكان حفظ أعمال الطلاب", delegate { using (var f = new StudentsFolderForm()) f.ShowDialog(this); }, false));
            side.Controls.Add(ActionButton("📋 سجل الدخول", OpenLog, false));
            side.Controls.Add(Section("الإعدادات"));
            side.Controls.Add(ActionButton("📝 طريقة فتح التمارين", delegate { using (var f = new OpenModeForm()) f.ShowDialog(this); }, false));
            side.Controls.Add(ActionButton("🔑 تغيير كلمة المرور", ChangePassword, false));
            side.Controls.Add(ActionButton("🖥️ إنشاء اختصار على سطح المكتب", CreateShortcut, false));

            var help = new Label
            {
                Dock = DockStyle.Top, Height = Ui.S(64), Padding = new Padding(Ui.S(16), Ui.S(8), Ui.S(16), 0),
                ForeColor = Ui.Muted, Font = Ui.F(9.5f),
                Text = "• كل ملف أو مجلد داخل الصف مباشرة = تمرين يظهر لجميع طلاب الصف باسمه.\n" +
                       "• ما يوضع داخل مجلد «علمي» أو «أدبي» يظهر لطلاب ذلك التخصص فقط.  • يمكنك أيضاً سحب الملفات وإفلاتها على الشجرة.",
            };

            tree.Dock = DockStyle.Fill;
            tree.Font = Ui.F(11f);
            tree.ItemHeight = Ui.S(28);
            tree.BorderStyle = BorderStyle.None;
            tree.HideSelection = false;
            tree.ShowLines = false;
            tree.AllowDrop = true;
            tree.BeforeExpand += (o, e) => Populate(e.Node);
            tree.AfterSelect += delegate { UpdateStatus(); };
            tree.NodeMouseDoubleClick += (o, e) => { if (e.Node.Tag is string && File.Exists((string)e.Node.Tag)) Ui.OpenInExplorer((string)e.Node.Tag, true); };
            tree.KeyDown += (o, e) => { if (e.KeyCode == Keys.Delete) Delete(); else if (e.KeyCode == Keys.F2) Rename(); };
            tree.DragEnter += (o, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            tree.DragOver += (o, e) =>
            {
                var n = NodeAtRow(tree.PointToClient(new Point(e.X, e.Y)).Y);
                if (n != null && tree.SelectedNode != n) tree.SelectedNode = n;
            };
            tree.DragDrop += (o, e) => Import((string[])e.Data.GetData(DataFormats.FileDrop));

            var treeHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Ui.S(12)), BackColor = Ui.Page };
            var treeBorder = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(Ui.S(8)) };
            treeBorder.Controls.Add(tree);
            treeHost.Controls.Add(treeBorder);

            status.Dock = DockStyle.Bottom;
            status.Height = Ui.S(30);
            status.BackColor = Color.White;
            status.ForeColor = Ui.Muted;
            status.Font = Ui.F(9f);
            status.TextAlign = ContentAlignment.MiddleRight;
            status.AutoEllipsis = true;

            Controls.Add(treeHost);
            Controls.Add(help);
            Controls.Add(side);
            Controls.Add(status);
            Controls.Add(header);

            LoadTree();
        }

        static Label Section(string text)
        {
            return new Label { Text = text, Font = Ui.FB(9.5f), ForeColor = Ui.Muted, AutoSize = true, Margin = new Padding(Ui.S(4), Ui.S(8), 0, Ui.S(4)) };
        }

        static Button ActionButton(string text, Action handler, bool primary)
        {
            var b = Ui.Btn(text, primary);
            b.Width = Ui.S(236);
            b.Height = Ui.S(36);
            b.TextAlign = ContentAlignment.MiddleRight;
            b.Margin = new Padding(0, 0, 0, Ui.S(4));
            b.Click += delegate { handler(); };
            return b;
        }

        // ---------- tree ----------

        void LoadTree()
        {
            string keep = SelectedPath();
            tree.BeginUpdate();
            tree.Nodes.Clear();
            var root = new TreeNode("📚 المكتبة") { Tag = AppPaths.Library };
            tree.Nodes.Add(root);
            foreach (var g in Catalog.Grades)
            {
                var n = new TreeNode("🎓 " + g.Name) { Tag = Catalog.GradeFolder(g), NodeFont = Ui.FB(11f) };
                root.Nodes.Add(n);
                Populate(n);
                n.Expand();
            }
            root.Expand();
            tree.EndUpdate();
            if (keep != null) Reselect(tree.Nodes, keep);
            if (tree.SelectedNode == null) tree.SelectedNode = root;
            UpdateStatus();
        }

        // The row under the pointer, judged by height only. GetNodeAt also checks X, which misses
        // in the mirrored right-to-left tree, so a drop used to land on the previously selected grade.
        TreeNode NodeAtRow(int y)
        {
            for (var n = tree.TopNode; n != null; n = n.NextVisibleNode)
            {
                var b = n.Bounds;
                if (y >= b.Top && y < b.Bottom) return n;
                if (b.Top > tree.ClientSize.Height) break;
            }
            return null;
        }

        void Reselect(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode n in nodes)
            {
                string p = n.Tag as string;
                if (p == null) continue;
                if (string.Equals(p, path, StringComparison.OrdinalIgnoreCase)) { tree.SelectedNode = n; return; }
                if (path.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    n.Expand();
                    Reselect(n.Nodes, path);
                    return;
                }
            }
        }

        void Populate(TreeNode node)
        {
            string dir = node.Tag as string;
            if (dir == null || !Directory.Exists(dir)) return;
            if (node.Nodes.Count > 0 && node.Nodes[0].Tag != null) return; // already loaded
            node.Nodes.Clear();

            bool isGrade = Catalog.Grades.Any(g => string.Equals(Catalog.GradeFolder(g), dir, StringComparison.OrdinalIgnoreCase));
            var dirs = Directory.GetDirectories(dir).OrderBy(d => Path.GetFileName(d), Natural).ToList();
            var files = Directory.GetFiles(dir).OrderBy(f => Path.GetFileName(f), Natural).ToList();

            if (isGrade)
            {
                // Track folders first so the admin sees where track-only material goes.
                foreach (var t in Catalog.Tracks)
                {
                    string p = Path.Combine(dir, t);
                    if (!Directory.Exists(p)) continue;
                    var tn = new TreeNode("🔖 " + t + " (خاص بطلاب " + t + ")") { Tag = p, ForeColor = Ui.NavyLight };
                    AddPlaceholder(tn);
                    node.Nodes.Add(tn);
                    dirs.Remove(p);
                }
            }
            foreach (var d in dirs) { var n = new TreeNode("📁 " + Path.GetFileName(d)) { Tag = d }; AddPlaceholder(n); node.Nodes.Add(n); }
            foreach (var f in files) node.Nodes.Add(new TreeNode("📄 " + Path.GetFileName(f)) { Tag = f });
        }

        static void AddPlaceholder(TreeNode n)
        {
            try { if (Directory.EnumerateFileSystemEntries((string)n.Tag).Any()) n.Nodes.Add(new TreeNode(Placeholder)); }
            catch { }
        }

        static readonly Comparer<string> Natural = Comparer<string>.Create(Catalog.NaturalCompare);

        string SelectedPath() { return tree.SelectedNode == null ? null : tree.SelectedNode.Tag as string; }

        // Folder that new files go into: the selected folder, or the selected file's folder.
        string TargetFolder()
        {
            string p = SelectedPath();
            if (p == null) return null;
            if (File.Exists(p)) p = Path.GetDirectoryName(p);
            if (string.Equals(p.TrimEnd('\\'), AppPaths.Library.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                Ui.Info("الرجاء اختيار الصف أولاً (العاشر أو الحادي عشر أو الثاني عشر) من الشجرة.");
                return null;
            }
            return p;
        }

        bool IsProtected(string p)
        {
            string x = p.TrimEnd('\\');
            if (string.Equals(x, AppPaths.Library.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var g in Catalog.Grades)
            {
                string gf = Catalog.GradeFolder(g);
                if (string.Equals(x, gf, StringComparison.OrdinalIgnoreCase)) return true;
                if (g.HasTracks && Catalog.Tracks.Any(t => string.Equals(x, Path.Combine(gf, t), StringComparison.OrdinalIgnoreCase))) return true;
            }
            return false;
        }

        void UpdateStatus()
        {
            string p = SelectedPath();
            if (p == null) { status.Text = ""; return; }
            string where = TargetDescription(p);
            status.Text = "  " + Ui.Ltr(p) + (where == null ? "" : "   —   " + where);
        }

        string TargetDescription(string p)
        {
            foreach (var g in Catalog.Grades)
            {
                string gf = Catalog.GradeFolder(g);
                if (!p.StartsWith(gf, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var t in Catalog.Tracks)
                    if (p.StartsWith(Path.Combine(gf, t), StringComparison.OrdinalIgnoreCase)) return "يظهر لطلاب " + g.Name + " - " + t;
                return "يظهر لجميع طلاب " + g.Name;
            }
            return null;
        }

        // ---------- actions ----------

        void AddFiles()
        {
            string target = TargetFolder();
            if (target == null) return;
            using (var d = new OpenFileDialog { Multiselect = true, Title = "اختر الملفات المراد إضافتها" })
                if (d.ShowDialog(this) == DialogResult.OK) Import(d.FileNames);
        }

        void AddFolder()
        {
            string target = TargetFolder();
            if (target == null) return;
            using (var d = new FolderBrowserDialog { Description = "اختر المجلد المراد إضافته (سيتم نسخه بالكامل)" })
                if (d.ShowDialog(this) == DialogResult.OK) Import(new[] { d.SelectedPath });
        }

        void Import(string[] paths)
        {
            string target = TargetFolder();
            if (target == null || paths == null || paths.Length == 0) return;
            string last = null;
            int added = 0;
            var macOnly = new List<string>();
            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (var src in paths)
                {
                    // "._name" files and folders holding only them are Mac metadata with no content.
                    if (Catalog.IsMacJunk(src.TrimEnd('\\')) || (Directory.Exists(src) && Catalog.IsMacOnlyFolder(src)))
                    {
                        macOnly.Add(Path.GetFileName(src.TrimEnd('\\')));
                        continue;
                    }
                    string name = Path.GetFileName(src.TrimEnd('\\'));
                    string dst = Path.Combine(target, name);
                    if (string.Equals(src.TrimEnd('\\'), dst, StringComparison.OrdinalIgnoreCase)) continue;
                    if (dst.StartsWith(src.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        Ui.Error("لا يمكن نسخ مجلد داخل نفسه.");
                        continue;
                    }
                    if ((File.Exists(dst) || Directory.Exists(dst)) &&
                        !Ui.Confirm("«" + name + "» موجود مسبقاً في هذا المكان.\nهل تريد استبداله؟")) continue;

                    if (Directory.Exists(src))
                    {
                        if (Directory.Exists(dst)) Directory.Delete(dst, true);
                        Catalog.CopyDirectory(src, dst);
                    }
                    else
                    {
                        File.Copy(src, dst, true);
                    }
                    last = dst;
                    added++;
                }
            }
            catch (Exception ex) { Ui.Error("تعذر النسخ:\n" + ex.Message); }
            finally { Cursor = Cursors.Default; }

            RefreshFolder(target);
            if (last != null) Reselect(tree.Nodes, last);

            if (macOnly.Count > 0)
            {
                string list = string.Join("، ", macOnly.Take(6).ToArray()) + (macOnly.Count > 6 ? " …" : "");
                Ui.Error((added == 0 ? "لم تتم إضافة أي ملف." : "تمت إضافة " + added + " فقط.") + "\n\n" +
                         "هذه ليست تمارين حقيقية:\n" + list + "\n\n" +
                         "الملفات التي يبدأ اسمها بـ «._» ينشئها جهاز Mac تلقائياً، وتحتوي فقط على معلومات " +
                         "(مثل رابط التحميل) وليس على الكود. انسخ الملفات الأصلية (مثل ex 1.py) من جهاز Mac ثم أضفها.");
            }
        }

        void NewFolder()
        {
            string target = TargetFolder();
            if (target == null) return;
            string name = Ui.Prompt("تمرين جديد", "اسم المجلد (يظهر للطلاب بنفس الاسم):", "", false);
            if (string.IsNullOrWhiteSpace(name)) return;
            string dst = Path.Combine(target, Catalog.SafeName(name));
            if (Directory.Exists(dst) || File.Exists(dst)) { Ui.Error("يوجد عنصر بهذا الاسم مسبقاً."); return; }
            try { Directory.CreateDirectory(dst); }
            catch (Exception ex) { Ui.Error(ex.Message); return; }
            RefreshFolder(target);
            Reselect(tree.Nodes, dst);
        }

        void Rename()
        {
            string p = SelectedPath();
            if (p == null) return;
            if (IsProtected(p)) { Ui.Info("لا يمكن إعادة تسمية المجلدات الأساسية (المكتبة، الصفوف، علمي، أدبي)."); return; }
            string old = Path.GetFileName(p);
            string name = Ui.Prompt("إعادة تسمية", "الاسم الجديد:", old, false);
            if (string.IsNullOrWhiteSpace(name) || name == old) return;
            string dst = Path.Combine(Path.GetDirectoryName(p), Catalog.SafeName(name));
            try
            {
                if (Directory.Exists(p))
                {
                    if (string.Equals(p, dst, StringComparison.OrdinalIgnoreCase))
                    {
                        // Case-only rename needs a hop through a temporary name.
                        string tmp = p + ".tmp-rename";
                        Directory.Move(p, tmp);
                        Directory.Move(tmp, dst);
                    }
                    else Directory.Move(p, dst);
                }
                else File.Move(p, dst);
            }
            catch (Exception ex) { Ui.Error("تعذرت إعادة التسمية:\n" + ex.Message); return; }
            RefreshFolder(Path.GetDirectoryName(p));
            Reselect(tree.Nodes, dst);
        }

        void Delete()
        {
            string p = SelectedPath();
            if (p == null) return;
            if (IsProtected(p)) { Ui.Info("لا يمكن حذف المجلدات الأساسية (المكتبة، الصفوف، علمي، أدبي)."); return; }
            // Network drives have no Recycle Bin, so deleting there is permanent.
            string warning = AppPaths.IsNetwork(p)
                ? "سيتم حذفه نهائياً من جميع الأجهزة ولا يمكن استرجاعه."
                : "سيتم نقله إلى سلة المحذوفات.";
            if (!Ui.Confirm("هل تريد حذف «" + Path.GetFileName(p) + "»؟\n" + warning)) return;
            try
            {
                if (Directory.Exists(p)) FileSystem.DeleteDirectory(p, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else FileSystem.DeleteFile(p, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Ui.Error("تعذر الحذف:\n" + ex.Message); }
            string parent = Path.GetDirectoryName(p);
            RefreshFolder(parent);
            Reselect(tree.Nodes, parent);
        }

        void ShowInExplorer()
        {
            string p = SelectedPath();
            if (p == null) return;
            if (File.Exists(p)) Ui.OpenInExplorer(p, true);
            else Ui.OpenInExplorer(p, false);
        }

        void OpenLog()
        {
            if (!File.Exists(AppPaths.LoginLog)) { Ui.Info("لا يوجد سجل دخول بعد."); return; }
            try { System.Diagnostics.Process.Start(AppPaths.LoginLog); }
            catch { Ui.OpenInExplorer(AppPaths.LoginLog, true); }
        }

        void ChangePassword()
        {
            string p1 = Ui.Prompt("تغيير كلمة المرور", "كلمة المرور الجديدة:", "", true);
            if (p1 == null) return;
            if (p1.Length < 4) { Ui.Error("يجب أن تتكون كلمة المرور من 4 أحرف على الأقل."); return; }
            string p2 = Ui.Prompt("تغيير كلمة المرور", "أعد كتابة كلمة المرور:", "", true);
            if (p2 == null) return;
            if (p1 != p2) { Ui.Error("كلمتا المرور غير متطابقتين."); return; }
            try { Settings.SetPassword(p1); Ui.Info("تم تغيير كلمة المرور."); }
            catch (Exception ex) { Ui.Error("تعذر حفظ كلمة المرور:\n" + ex.Message); }
        }

        void CreateShortcut()
        {
            try
            {
                Shortcuts.CreateAll();
                Ui.Info("تم إنشاء اختصار «" + Shortcuts.Name + "» على سطح المكتب وفي قائمة ابدأ.");
            }
            catch (Exception ex) { Ui.Error("تعذر إنشاء الاختصار:\n" + ex.Message); }
        }

        // Reloads the children of the node that shows the given folder.
        void RefreshFolder(string dir)
        {
            var node = Find(tree.Nodes, dir);
            if (node == null) { LoadTree(); return; }
            tree.BeginUpdate();
            node.Nodes.Clear();
            node.Nodes.Add(new TreeNode(Placeholder));
            Populate(node);
            node.Expand();
            tree.EndUpdate();
            UpdateStatus();
        }

        static TreeNode Find(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode n in nodes)
            {
                string p = n.Tag as string;
                if (p == null) continue;
                if (string.Equals(p.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return n;
                var r = Find(n.Nodes, path);
                if (r != null) return r;
            }
            return null;
        }
    }
}
