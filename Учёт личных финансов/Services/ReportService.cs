using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using FinanceTracker.Controllers;
using FinanceTracker.Models;

namespace FinanceTracker.Services
{
    // Отчёт за период через печать. В окне предпросмотра можно выбрать принтер
    // «Microsoft Print to PDF» и сохранить отчёт в PDF.
    public static class ReportService
    {
        public static void ShowPreview(List<Transaction> rows, DateTime from, DateTime to, FinanceController controller)
        {
            var printer = new ReportPrinter(rows, from, to, controller);
            printer.Show();
        }

        private class ReportPrinter
        {
            private readonly List<Transaction> rows;
            private readonly DateTime from;
            private readonly DateTime to;
            private readonly FinanceController controller;
            private int index;

            public ReportPrinter(List<Transaction> rows, DateTime from, DateTime to, FinanceController controller)
            {
                this.rows = rows;
                this.from = from;
                this.to = to;
                this.controller = controller;
            }

            public void Show()
            {
                var doc = new PrintDocument { DocumentName = "Отчёт по финансам" };
                doc.PrintPage += PrintPage;
                index = 0;

                using (var dlg = new PrintPreviewDialog
                {
                    Document = doc,
                    Width = 900,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterScreen
                })
                {
                    dlg.ShowDialog();
                }
            }

            private void PrintPage(object sender, PrintPageEventArgs e)
            {
                var g = e.Graphics;
                float left = e.MarginBounds.Left;
                float right = e.MarginBounds.Right;
                float y = e.MarginBounds.Top;

                using (var titleFont = new Font("Segoe UI", 14, FontStyle.Bold))
                using (var subFont = new Font("Segoe UI", 9))
                using (var headFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var cellFont = new Font("Segoe UI", 8.5f))
                {
                    if (index == 0)
                    {
                        g.DrawString("Отчёт по операциям", titleFont, Brushes.Black, left, y);
                        y += 26;
                        g.DrawString(string.Format("за период {0:dd.MM.yyyy} — {1:dd.MM.yyyy}", from, to),
                            subFont, Brushes.Gray, left, y);
                        y += 24;
                    }

                    float[] cx = { left, left + 70, left + 130, left + 250, left + 350, left + 440 };

                    // шапка таблицы
                    g.DrawString("Дата", headFont, Brushes.Black, cx[0], y);
                    g.DrawString("Тип", headFont, Brushes.Black, cx[1], y);
                    g.DrawString("Категория", headFont, Brushes.Black, cx[2], y);
                    g.DrawString("Счёт", headFont, Brushes.Black, cx[3], y);
                    g.DrawString("Сумма", headFont, Brushes.Black, cx[4], y);
                    g.DrawString("Комментарий", headFont, Brushes.Black, cx[5], y);
                    y += 16;
                    g.DrawLine(Pens.LightGray, left, y, right, y);
                    y += 4;

                    while (index < rows.Count && y < e.MarginBounds.Bottom - 30)
                    {
                        var t = rows[index];
                        var cat = controller.CategoryById(t.CategoryId);
                        var acc = controller.AccountById(t.AccountId);

                        g.DrawString(t.Date.ToString("dd.MM.yyyy"), cellFont, Brushes.Black, cx[0], y);
                        g.DrawString(t.Type == TransactionType.Income ? "доход" : "расход", cellFont, Brushes.Black, cx[1], y);
                        g.DrawString(cat != null ? cat.Name : "—", cellFont, Brushes.Black, cx[2], y);
                        g.DrawString(acc != null ? acc.Name : "—", cellFont, Brushes.Black, cx[3], y);

                        var amountBrush = t.Type == TransactionType.Income ? Brushes.SeaGreen : Brushes.Firebrick;
                        g.DrawString(t.DisplayAmount, cellFont, amountBrush, cx[4], y);
                        g.DrawString(Trim(t.Description, 28), cellFont, Brushes.Black, cx[5], y);

                        y += 16;
                        index++;
                    }

                    if (index >= rows.Count)
                    {
                        y += 6;
                        g.DrawLine(Pens.LightGray, left, y, right, y);
                        y += 8;

                        decimal income = rows.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                        decimal expense = rows.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

                        g.DrawString(string.Format("Доходы: {0:N2} \u20BD", income), headFont, Brushes.SeaGreen, cx[0], y);
                        y += 16;
                        g.DrawString(string.Format("Расходы: {0:N2} \u20BD", expense), headFont, Brushes.Firebrick, cx[0], y);
                        y += 16;
                        g.DrawString(string.Format("Итог: {0:N2} \u20BD", income - expense), headFont, Brushes.Black, cx[0], y);

                        e.HasMorePages = false;
                    }
                    else
                    {
                        e.HasMorePages = true;
                    }
                }
            }

            private static string Trim(string s, int max)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
            }
        }
    }
}
