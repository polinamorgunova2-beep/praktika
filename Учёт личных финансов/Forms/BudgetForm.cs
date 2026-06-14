using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FinanceTracker.Controllers;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    // Лимиты расходов по категориям на текущий месяц + наглядно видно, где перерасход.
    public class BudgetForm : Form
    {
        private readonly FinanceController controller;
        private readonly int month = DateTime.Today.Month;
        private readonly int year = DateTime.Today.Year;

        private DataGridView grid;

        public BudgetForm(FinanceController controller)
        {
            this.controller = controller;
            BuildUi();
            LoadData();
        }

        private void BuildUi()
        {
            Text = "Бюджеты на " + new DateTime(year, month, 1).ToString("MMMM yyyy");
            Width = 640;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cat", HeaderText = "Категория", ReadOnly = true, FillWeight = 28 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "limit", HeaderText = "Лимит, \u20BD", FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "spent", HeaderText = "Потрачено", ReadOnly = true, FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "left", HeaderText = "Остаток", ReadOnly = true, FillWeight = 22 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pct", HeaderText = "%", ReadOnly = true, FillWeight = 10 });

            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Впишите лимит в рублях напротив категории и нажмите «Сохранить лимиты».",
                ForeColor = Color.Gray,
                Padding = new Padding(8, 6, 0, 0)
            };

            var btnSave = new Button { Text = "Сохранить лимиты", Dock = DockStyle.Bottom, Height = 34 };
            btnSave.Click += (s, e) => SaveLimits();

            Controls.Add(grid);
            Controls.Add(btnSave);
            Controls.Add(hint);
        }

        private void LoadData()
        {
            grid.Rows.Clear();

            foreach (var c in controller.Categories.Where(x => x.Type == TransactionType.Expense))
            {
                var budget = controller.Budgets.FirstOrDefault(b => b.CategoryId == c.Id && b.Month == month && b.Year == year);
                decimal limit = budget != null ? budget.Limit : 0;
                decimal spent = controller.SpentInCategory(c.Id, month, year);
                decimal left = limit - spent;
                int pct = limit > 0 ? (int)(spent / limit * 100) : 0;

                int i = grid.Rows.Add(
                    c.Name,
                    limit > 0 ? limit.ToString("0") : "",
                    spent.ToString("N0"),
                    limit > 0 ? left.ToString("N0") : "—",
                    limit > 0 ? pct + "%" : "—");

                grid.Rows[i].Tag = c.Id;

                if (limit > 0 && spent > limit)
                    grid.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                else if (limit > 0 && pct >= 80)
                    grid.Rows[i].DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
            }
        }

        private void SaveLimits()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                int catId = (int)row.Tag;
                var raw = row.Cells["limit"].Value;

                decimal limit;
                if (raw != null && decimal.TryParse(raw.ToString(), out limit) && limit > 0)
                    controller.SetBudget(catId, month, year, limit);
            }

            LoadData();
            MessageBox.Show("Лимиты сохранены.", "Бюджет", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
