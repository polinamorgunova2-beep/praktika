using System;
using System.Drawing;
using System.Windows.Forms;

namespace FinanceTracker.Forms
{
    // Окно с диаграммой классов прямо внутри приложения (встроенный просмотрщик).
    public class DiagramForm : Form
    {
        public DiagramForm(string htmlPath)
        {
            Text = "Диаграмма классов";
            Width = 1120;
            Height = 800;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);

            var browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false
            };
            try { browser.Url = new Uri(htmlPath); }
            catch { /* путь некорректен — окно просто откроется пустым */ }

            Controls.Add(browser);
        }
    }
}
