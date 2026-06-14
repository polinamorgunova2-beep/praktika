// Главное окно приложения «Учёт личных финансов».
// Боковая панель навигации + цветная шапка с балансом + таблица операций.
// Моргунова

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FinanceTracker.Controllers;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public class MainForm : Form
    {
        private readonly FinanceController controller;
        private readonly UserRole role;

        // палитра
        private static readonly Color Sidebar = Color.FromArgb(31, 41, 55);
        private static readonly Color SidebarHover = Color.FromArgb(55, 65, 81);
        private static readonly Color Accent = Color.FromArgb(79, 70, 229);
        private static readonly Color LightText = Color.FromArgb(229, 231, 235);
        private static readonly Color MutedText = Color.FromArgb(156, 163, 175);
        private static readonly Color PageBg = Color.FromArgb(247, 248, 251);

        private DataGridView grid;
        private Label lblBalance;
        private Label lblIncome;
        private Label lblExpense;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;

        private Button btnUndo;
        private Button btnRedo;
        private Button btnEdit;
        private Button btnDel;
        private Button navAdd;
        private Button navDemo;

        public MainForm(UserRole role)
        {
            this.role = role;
            controller = new FinanceController();
            controller.Changed += (s, e) => RefreshAll();
            controller.BudgetExceeded += (s, msg) =>
                MessageBox.Show(msg, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            BuildUi();
            ApplyRole();
            RefreshAll();
        }

        private void BuildUi()
        {
            Text = "Личные финансы";
            Width = 1060;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = PageBg;
            KeyPreview = true;
            KeyDown += OnKeyDown;

            Controls.Add(BuildContent());   // Fill — добавляем первым
            Controls.Add(BuildSidebar());   // Left
        }

        // ---------- боковая панель ----------

        private Panel BuildSidebar()
        {
            var side = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Sidebar };

            var title = new Label
            {
                Text = "Личные\nфинансы",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(20, 14, 0, 0)
            };

            var author = new Label
            {
                Text = "Моргунова Полина",
                ForeColor = MutedText,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(22, 0, 0, 0)
            };

            var roleLabel = new Label
            {
                Text = role == UserRole.Admin ? "Роль: администратор" : "Роль: гость (просмотр)",
                ForeColor = role == UserRole.Admin ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 211, 77),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(22, 2, 0, 0),
                Font = new Font("Segoe UI", 8.5f)
            };

            navAdd = NavButton("＋   Добавить операцию", (s, e) => AddTransaction());
            var navStats = NavButton("📊   Статистика", (s, e) => OpenDialog(new StatisticsForm(controller)));
            var navBudget = NavButton("🎯   Бюджеты", (s, e) => { OpenDialog(new BudgetForm(controller)); RefreshAll(); });
            var navReport = NavButton("🖨   Отчёт (печать / PDF)", (s, e) => ShowReport());
            navDemo = NavButton("✨   Заполнить примерами", (s, e) => FillDemo());
            var navDiagram = NavButton("📐   Диаграмма классов", (s, e) => OpenClassDiagram());

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 88, BackColor = Sidebar };
            btnUndo = SmallSideButton("↶ Отменить", 12);
            btnUndo.Click += (s, e) => controller.Undo();
            btnRedo = SmallSideButton("↷ Повторить", 48);
            btnRedo.Click += (s, e) => controller.Redo();
            bottom.Controls.Add(btnUndo);
            bottom.Controls.Add(btnRedo);

            // добавляем в обратном порядке для Dock=Top
            side.Controls.Add(navDiagram);
            side.Controls.Add(navDemo);
            side.Controls.Add(navReport);
            side.Controls.Add(navBudget);
            side.Controls.Add(navStats);
            side.Controls.Add(navAdd);
            side.Controls.Add(roleLabel);
            side.Controls.Add(author);
            side.Controls.Add(title);
            side.Controls.Add(bottom);

            return side;
        }

        private Button NavButton(string text, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                ForeColor = LightText,
                BackColor = Sidebar,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10.5f),
                Padding = new Padding(18, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = SidebarHover;
            b.Click += onClick;
            return b;
        }

        private Button SmallSideButton(string text, int top)
        {
            var b = new Button
            {
                Text = text,
                Left = 16,
                Top = top,
                Width = 178,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = LightText,
                BackColor = SidebarHover,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ---------- правая часть ----------

        private Panel BuildContent()
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = PageBg };

            content.Controls.Add(BuildGridArea());   // Fill
            content.Controls.Add(BuildToolbar());     // Top
            content.Controls.Add(BuildHeader());      // Top

            return content;
        }

        private Panel BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Accent };

            var caption = new Label
            {
                Text = "Баланс на всех счетах",
                ForeColor = Color.FromArgb(199, 210, 254),
                AutoSize = true,
                Location = new Point(24, 18),
                Font = new Font("Segoe UI", 10f)
            };
            lblBalance = new Label
            {
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(22, 40),
                Font = new Font("Segoe UI", 26f, FontStyle.Bold)
            };

            lblIncome = StatLabel(header, 560, "Доходы за месяц");
            lblExpense = StatLabel(header, 770, "Расходы за месяц");

            header.Controls.Add(caption);
            header.Controls.Add(lblBalance);
            return header;
        }

        private Label StatLabel(Panel host, int x, string caption)
        {
            var cap = new Label
            {
                Text = caption,
                ForeColor = Color.FromArgb(199, 210, 254),
                AutoSize = true,
                Location = new Point(x, 30),
                Font = new Font("Segoe UI", 9f)
            };
            var val = new Label
            {
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(x, 50),
                Font = new Font("Segoe UI", 17f, FontStyle.Bold)
            };
            host.Controls.Add(cap);
            host.Controls.Add(val);
            return val;
        }

        private Panel BuildToolbar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = PageBg, Padding = new Padding(20, 10, 20, 6) };

            var lblPeriod = new Label { Text = "Период:", AutoSize = true, Location = new Point(22, 22), ForeColor = Color.FromArgb(75, 85, 99) };

            // период по умолчанию — последние полгода, чтобы сразу были видны операции
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
            dtpFrom = new DateTimePicker { Width = 120, Location = new Point(78, 18), Value = start };
            var dash = new Label { Text = "—", AutoSize = true, Location = new Point(204, 22) };
            dtpTo = new DateTimePicker { Width = 120, Location = new Point(222, 18), Value = DateTime.Today };
            dtpFrom.ValueChanged += (s, e) => RefreshGrid();
            dtpTo.ValueChanged += (s, e) => RefreshGrid();

            btnEdit = FlatButton("Изменить", Color.FromArgb(99, 102, 241), 360);
            btnEdit.Click += (s, e) => EditSelected();
            btnDel = FlatButton("Удалить", Color.FromArgb(239, 68, 68), 470);
            btnDel.Click += (s, e) => DeleteSelected();

            bar.Controls.Add(lblPeriod);
            bar.Controls.Add(dtpFrom);
            bar.Controls.Add(dash);
            bar.Controls.Add(dtpTo);
            bar.Controls.Add(btnEdit);
            bar.Controls.Add(btnDel);
            return bar;
        }

        private static Button FlatButton(string text, Color color, int x)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, 15),
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = color,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Panel BuildGridArea()
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = PageBg, Padding = new Padding(20, 4, 20, 20) };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(236, 238, 242),
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            grid.RowTemplate.Height = 30;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(107, 114, 128);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 41, 55);
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
            BuildColumns();

            wrap.Controls.Add(grid);
            return wrap;
        }

        private void BuildColumns()
        {
            grid.Columns.Clear();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "№", FillWeight = 6 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Дата", FillWeight = 14 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat", HeaderText = "Категория", FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAcc", HeaderText = "Счёт", FillWeight = 16 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Комментарий", FillWeight = 28 });
            var amount = new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Сумма", FillWeight = 16 };
            amount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            amount.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.Columns.Add(amount);
        }

        // ---------- роли ----------

        private void ApplyRole()
        {
            if (role == UserRole.Admin) return;

            // гость — только просмотр: отключаем всё, что меняет данные
            navAdd.Enabled = false;
            navDemo.Enabled = false;
            btnEdit.Enabled = false;
            btnDel.Enabled = false;
            btnUndo.Enabled = false;
            btnRedo.Enabled = false;

            foreach (var b in new[] { navAdd, navDemo })
                b.ForeColor = Color.FromArgb(107, 114, 128);
        }

        private bool EnsureCanEdit()
        {
            if (role == UserRole.Guest)
            {
                Info("Гость может только просматривать данные. Войдите как администратор, чтобы вносить изменения.");
                return false;
            }
            return true;
        }

        // ---------- обновление данных ----------

        private void RefreshAll()
        {
            RefreshCards();
            RefreshGrid();
            if (role == UserRole.Admin)
            {
                if (btnUndo != null) btnUndo.Enabled = controller.CanUndo;
                if (btnRedo != null) btnRedo.Enabled = controller.CanRedo;
            }
        }

        private void RefreshCards()
        {
            decimal income, expense;
            controller.MonthlyTotals(DateTime.Today.Month, DateTime.Today.Year, out income, out expense);
            lblBalance.Text = controller.TotalBalance().ToString("N0") + " \u20BD";
            lblIncome.Text = income.ToString("N0") + " \u20BD";
            lblExpense.Text = expense.ToString("N0") + " \u20BD";
        }

        private void RefreshGrid()
        {
            if (grid == null) return;
            grid.Rows.Clear();

            foreach (var t in controller.InPeriod(dtpFrom.Value, dtpTo.Value))
            {
                var cat = controller.CategoryById(t.CategoryId);
                var acc = controller.AccountById(t.AccountId);

                int i = grid.Rows.Add(
                    t.Id,
                    t.Date.ToString("dd.MM.yyyy"),
                    cat != null ? cat.Name : "—",
                    acc != null ? acc.Name : "—",
                    t.Description,
                    t.DisplayAmount + " \u20BD");

                grid.Rows[i].Cells["colAmount"].Style.ForeColor = t.AmountColor;
            }
        }

        // ---------- действия ----------

        private void OpenDialog(Form f)
        {
            using (f) f.ShowDialog(this);
        }

        private int? SelectedId()
        {
            if (grid.CurrentRow == null) return null;
            return (int)grid.CurrentRow.Cells["colId"].Value;
        }

        private void AddTransaction()
        {
            if (!EnsureCanEdit()) return;
            using (var f = new TransactionForm(controller))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    controller.AddTransaction(f.Result);
            }
        }

        private void EditSelected()
        {
            if (!EnsureCanEdit()) return;
            var id = SelectedId();
            if (id == null) { Info("Выберите операцию в таблице."); return; }

            var existing = controller.Transactions.FirstOrDefault(t => t.Id == id.Value);
            if (existing == null) return;

            using (var f = new TransactionForm(controller, existing))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    controller.UpdateTransaction(f.Result);
            }
        }

        private void DeleteSelected()
        {
            if (!EnsureCanEdit()) return;
            var id = SelectedId();
            if (id == null) { Info("Выберите операцию в таблице."); return; }

            if (MessageBox.Show("Удалить выбранную операцию?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                controller.DeleteTransaction(id.Value);
            }
        }

        private void FillDemo()
        {
            if (!EnsureCanEdit()) return;
            if (MessageBox.Show("Создать заново набор примеров операций за последние полгода?\n(текущие операции будут заменены)",
                    "Демо-данные", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                controller.GenerateDemoData();
            }
        }

        private void ShowReport()
        {
            var rows = controller.InPeriod(dtpFrom.Value, dtpTo.Value);
            if (rows.Count == 0) { Info("За выбранный период нет операций."); return; }
            Services.ReportService.ShowPreview(rows, dtpFrom.Value, dtpTo.Value, controller);
        }

        private void OpenClassDiagram()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = System.IO.Path.Combine(baseDir, "docs", "Диаграмма-классов.html");

                if (!System.IO.File.Exists(path))
                {
                    // во время разработки файл лежит в папке проекта (выше папки bin)
                    string alt = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(baseDir, "..", "..", "..", "docs", "Диаграмма-классов.html"));
                    if (System.IO.File.Exists(alt)) path = alt;
                }

                if (System.IO.File.Exists(path))
                {
                    using (var f = new DiagramForm(path)) f.ShowDialog(this);
                }
                else
                    Info("Файл диаграммы не найден. Откройте docs/Диаграмма-классов.html вручную.");
            }
            catch (Exception ex)
            {
                Info("Не удалось открыть диаграмму: " + ex.Message);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (role != UserRole.Admin) return;
            if (e.Control && e.KeyCode == Keys.Z) { controller.Undo(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Y) { controller.Redo(); e.Handled = true; }
        }

        private static void Info(string text)
        {
            MessageBox.Show(text, "Личные финансы", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
