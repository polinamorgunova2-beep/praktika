using System.Collections.Generic;
using System.IO;
using System.Text;
using FinanceTracker.Controllers;
using FinanceTracker.Models;

namespace FinanceTracker.Services
{
    public static class ExportService
    {
        // Выгрузка операций в CSV (нормально открывается Excel-ом).
        public static void SaveCsv(string path, List<Transaction> rows, FinanceController controller)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Дата;Тип;Категория;Счёт;Сумма;Комментарий");

            foreach (var t in rows)
            {
                var cat = controller.CategoryById(t.CategoryId);
                var acc = controller.AccountById(t.AccountId);
                string type = t.Type == TransactionType.Income ? "доход" : "расход";

                sb.AppendLine(string.Join(";", new[]
                {
                    t.Date.ToString("dd.MM.yyyy"),
                    type,
                    cat != null ? cat.Name : "",
                    acc != null ? acc.Name : "",
                    t.Amount.ToString("0.00"),
                    Escape(t.Description)
                }));
            }

            // BOM, чтобы кириллица в Excel не превратилась в кракозябры
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(";") || s.Contains("\""))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
