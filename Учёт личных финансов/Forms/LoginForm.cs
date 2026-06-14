using System;
using System.Drawing;
using System.Windows.Forms;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    // Окно входа. Администратор входит по паролю и получает полный доступ,
    // гость заходит без пароля и может только просматривать данные.
    public class LoginForm : Form
    {
        private TextBox txtPassword;

        public UserRole Role { get; private set; }

        // Пароль администратора (для учебного проекта задан в коде).
        private const string AdminPassword = "admin";

        public LoginForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Вход — Личные финансы";
            Width = 380;
            Height = 280;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;

            var title = new Label
            {
                Text = "Личные финансы",
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 70, 229),
                AutoSize = true,
                Location = new Point(24, 22)
            };

            var hint = new Label
            {
                Text = "Выберите режим входа:",
                ForeColor = Color.FromArgb(75, 85, 99),
                AutoSize = true,
                Location = new Point(26, 60)
            };

            var lblPwd = new Label { Text = "Пароль администратора:", AutoSize = true, Location = new Point(26, 92) };
            txtPassword = new TextBox { Location = new Point(28, 112), Width = 300, UseSystemPasswordChar = true };
            var pwdHint = new Label
            {
                Text = "(для проверки: admin)",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(28, 138),
                Font = new Font("Segoe UI", 8f)
            };

            var btnAdmin = new Button
            {
                Text = "Войти как администратор",
                Location = new Point(28, 168),
                Width = 300,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAdmin.FlatAppearance.BorderSize = 0;
            btnAdmin.Click += (s, e) => LoginAsAdmin();

            var btnGuest = new Button
            {
                Text = "Войти как гость (только просмотр)",
                Location = new Point(28, 208),
                Width = 300,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(229, 231, 235),
                ForeColor = Color.FromArgb(31, 41, 55),
                Cursor = Cursors.Hand
            };
            btnGuest.FlatAppearance.BorderSize = 0;
            btnGuest.Click += (s, e) => { Role = UserRole.Guest; DialogResult = DialogResult.OK; Close(); };

            AcceptButton = btnAdmin;

            Controls.Add(title);
            Controls.Add(hint);
            Controls.Add(lblPwd);
            Controls.Add(txtPassword);
            Controls.Add(pwdHint);
            Controls.Add(btnAdmin);
            Controls.Add(btnGuest);
        }

        private void LoginAsAdmin()
        {
            if (txtPassword.Text == AdminPassword)
            {
                Role = UserRole.Admin;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Неверный пароль администратора.", "Вход",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }
    }
}
