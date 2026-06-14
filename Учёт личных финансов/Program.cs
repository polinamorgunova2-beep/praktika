// Учёт личных финансов — итоговый проект
// Моргунова
//
// Точка входа: сначала окно входа (выбор роли), затем главное окно.

using System;
using System.Windows.Forms;
using FinanceTracker.Forms;

namespace FinanceTracker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;

                Application.Run(new MainForm(login.Role));
            }
        }
    }
}
