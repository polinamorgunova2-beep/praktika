using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FinanceTracker.Controllers;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    // Диалог добавления и редактирования операции.
    public class TransactionForm : Form
    {
        private readonly FinanceController controller;
        private readonly Transaction editing;   // null => создаём новую

        private ComboBox cboType;
        private ComboBox cboCategory;
        private ComboBox cboAccount;
        private DateTimePicker dtpDate;
        private NumericUpDown numAmount;
        private TextBox txtDesc;

        public Transaction Result { get; private set; }

        public TransactionForm(FinanceController controller) : this(controller, null) { }

        public TransactionForm(FinanceController controller, Transaction toEdit)
        {
            this.controller = controller;
            this.editing = toEdit;

            BuildUi();
            FillCategories();
            if (editing != null) LoadFromTransaction();
        }

        private void BuildUi()
        {
            Text = editing == null ? "Новая операция" : "Изменение операции";
            Width = 380;
            Height = 330;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5f);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboType.Items.Add("Расход");
            cboType.Items.Add("Доход");
            cboType.SelectedIndex = 0;
            cboType.SelectedIndexChanged += (s, e) => FillCategories();

            cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };

            cboAccount = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var a in controller.Accounts) cboAccount.Items.Add(a);
            if (cboAccount.Items.Count > 0) cboAccount.SelectedIndex = 0;

            dtpDate = new DateTimePicker { Dock = DockStyle.Fill, Value = DateTime.Today, Format = DateTimePickerFormat.Short };
            numAmount = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true };
            txtDesc = new TextBox { Dock = DockStyle.Fill };

            AddRow(layout, 0, "Тип", cboType);
            AddRow(layout, 1, "Категория", cboCategory);
            AddRow(layout, 2, "Счёт", cboAccount);
            AddRow(layout, 3, "Дата", dtpDate);
            AddRow(layout, 4, "Сумма, \u20BD", numAmount);
            AddRow(layout, 5, "Комментарий", txtDesc);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "Сохранить", Width = 100 };
            var btnCancel = new Button { Text = "Отмена", Width = 80, DialogResult = DialogResult.Cancel };
            btnOk.Click += (s, e) => Save();
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 1, 6);

            Controls.Add(layout);
            CancelButton = btnCancel;
        }

        private static void AddRow(TableLayoutPanel layout, int row, string caption, Control control)
        {
            layout.Controls.Add(new Label { Text = caption, Anchor = AnchorStyles.Left, AutoSize = true, Padding = new Padding(0, 4, 0, 0) }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void FillCategories()
        {
            if (cboCategory == null) return;

            var type = cboType.SelectedIndex == 1 ? TransactionType.Income : TransactionType.Expense;
            cboCategory.Items.Clear();
            foreach (var c in controller.Categories.Where(c => c.Type == type))
                cboCategory.Items.Add(c);
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
        }

        private void LoadFromTransaction()
        {
            cboType.SelectedIndex = editing.Type == TransactionType.Income ? 1 : 0;
            FillCategories();
            SelectInCombo(cboCategory, o => ((Category)o).Id == editing.CategoryId);
            SelectInCombo(cboAccount, o => ((Account)o).Id == editing.AccountId);
            dtpDate.Value = editing.Date;
            numAmount.Value = Math.Min(editing.Amount, numAmount.Maximum);
            txtDesc.Text = editing.Description;
        }

        private static void SelectInCombo(ComboBox box, Func<object, bool> match)
        {
            for (int i = 0; i < box.Items.Count; i++)
            {
                if (match(box.Items[i])) { box.SelectedIndex = i; return; }
            }
        }

        private void Save()
        {
            if (cboCategory.SelectedItem == null) { MessageBox.Show("Выберите категорию."); return; }
            if (cboAccount.SelectedItem == null) { MessageBox.Show("Выберите счёт."); return; }
            if (numAmount.Value <= 0) { MessageBox.Show("Сумма должна быть больше нуля."); return; }

            Result = new Transaction
            {
                Id = editing != null ? editing.Id : 0,
                Type = cboType.SelectedIndex == 1 ? TransactionType.Income : TransactionType.Expense,
                CategoryId = ((Category)cboCategory.SelectedItem).Id,
                AccountId = ((Account)cboAccount.SelectedItem).Id,
                Date = dtpDate.Value.Date,
                Amount = numAmount.Value,
                Description = txtDesc.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
