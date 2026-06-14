using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace FinanceTracker.Forms
{
    // Графики рисую вручную через GDI+: круговая диаграмма расходов по категориям
    // и столбчатая диаграмма доход/расход за последние полгода.
    public class StatisticsForm : Form
    {
        private readonly Controllers.FinanceController controller;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Panel pie;
        private Panel bars;

        public StatisticsForm(Controllers.FinanceController controller)
        {
            this.controller = controller;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Статистика";
            Width = 880;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(10, 8, 0, 0) };
            dtpFrom = new DateTimePicker { Width = 120, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpTo = new DateTimePicker { Width = 120, Value = DateTime.Today };
            var btn = new Button { Text = "Обновить", Width = 100 };
            btn.Click += (s, e) => { pie.Invalidate(); bars.Invalidate(); };

            top.Controls.Add(new Label { Text = "Расходы с:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
            top.Controls.Add(dtpFrom);
            top.Controls.Add(new Label { Text = "по", AutoSize = true, Padding = new Padding(6, 6, 4, 0) });
            top.Controls.Add(dtpTo);
            top.Controls.Add(btn);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            pie = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            bars = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pie.Paint += PaintPie;
            bars.Paint += PaintBars;
            split.Panel1.Controls.Add(pie);
            split.Panel2.Controls.Add(bars);

            Controls.Add(split);
            Controls.Add(top);

            Shown += (s, e) => { try { split.SplitterDistance = split.Height / 2; } catch { } };
        }

        private void PaintPie(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            g.DrawString("Расходы по категориям", new Font("Segoe UI", 11f, FontStyle.Bold), Brushes.Black, 12, 8);

            var data = controller.ExpensesByCategory(dtpFrom.Value, dtpTo.Value)
                                  .OrderByDescending(kv => kv.Value)
                                  .ToList();

            if (data.Count == 0)
            {
                g.DrawString("Нет расходов за выбранный период", Font, Brushes.Gray, 16, 42);
                return;
            }

            decimal total = data.Sum(d => d.Value);
            var rect = new Rectangle(20, 42, 210, 210);
            float start = -90f;
            int legendY = 46;

            foreach (var kv in data)
            {
                float sweep = (float)(kv.Value / total) * 360f;
                Color color = ParseColor(kv.Key.Color);

                using (var brush = new SolidBrush(color))
                    g.FillPie(brush, rect, start, sweep);
                start += sweep;

                using (var box = new SolidBrush(color))
                    g.FillRectangle(box, 260, legendY, 14, 14);

                int percent = (int)Math.Round(kv.Value / total * 100);
                g.DrawString(string.Format("{0} — {1:N0} \u20BD ({2}%)", kv.Key.Name, kv.Value, percent),
                    Font, Brushes.Black, 280, legendY - 1);
                legendY += 22;
            }
        }

        private void PaintBars(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            g.DrawString("Доходы и расходы по месяцам", new Font("Segoe UI", 11f, FontStyle.Bold), Brushes.Black, 12, 8);

            var anchor = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var months = new List<DateTime>();
            for (int i = 5; i >= 0; i--) months.Add(anchor.AddMonths(-i));

            var pairs = new List<Tuple<DateTime, decimal, decimal>>();
            decimal max = 1;
            foreach (var m in months)
            {
                decimal inc, exp;
                controller.MonthlyTotals(m.Month, m.Year, out inc, out exp);
                pairs.Add(Tuple.Create(m, inc, exp));
                max = Math.Max(max, Math.Max(inc, exp));
            }

            int chartTop = 44;
            int chartBottom = panel.Height - 34;
            int usable = Math.Max(20, chartBottom - chartTop);
            int slot = (panel.Width - 50) / months.Count;
            int x = 40;

            using (var incBrush = new SolidBrush(Color.FromArgb(76, 175, 80)))
            using (var expBrush = new SolidBrush(Color.FromArgb(229, 115, 115)))
            {
                foreach (var p in pairs)
                {
                    int hInc = (int)(p.Item2 / max * usable);
                    int hExp = (int)(p.Item3 / max * usable);
                    int barW = Math.Min(26, slot / 3);

                    g.FillRectangle(incBrush, x, chartBottom - hInc, barW, hInc);
                    g.FillRectangle(expBrush, x + barW + 4, chartBottom - hExp, barW, hExp);
                    g.DrawString(p.Item1.ToString("MMM"), Font, Brushes.Gray, x, chartBottom + 4);
                    x += slot;
                }
            }

            // легенда
            using (var b1 = new SolidBrush(Color.FromArgb(76, 175, 80)))
            using (var b2 = new SolidBrush(Color.FromArgb(229, 115, 115)))
            {
                g.FillRectangle(b1, 200, 22, 12, 12);
                g.DrawString("доходы", Font, Brushes.Black, 216, 20);
                g.FillRectangle(b2, 300, 22, 12, 12);
                g.DrawString("расходы", Font, Brushes.Black, 316, 20);
            }
        }

        private static Color ParseColor(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex)) return Color.Gray;
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return Color.Gray;
            }
        }
    }
}
