using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.Controllers
{
    public class FinanceController
    {
        private readonly DataService data;

        private BindingList<Transaction> transactions;
        private BindingList<Account> accounts;
        private BindingList<Category> categories;
        private List<Budget> budgets;

        private int nextTransactionId = 1;

        // история для отмены/повтора (храним снимки состояния в JSON)
        private readonly Stack<string> undoStack = new Stack<string>();
        private readonly Stack<string> redoStack = new Stack<string>();

        public event EventHandler Changed;
        public event EventHandler<string> BudgetExceeded;

        public FinanceController() : this(new DataService(), true) { }

        public FinanceController(DataService dataService) : this(dataService, true) { }

        // seedDemoIfEmpty = false используется в тестах, чтобы не создавались демо-данные
        public FinanceController(DataService dataService, bool seedDemoIfEmpty)
        {
            data = dataService;
            Load(seedDemoIfEmpty);
        }

        public BindingList<Transaction> Transactions { get { return transactions; } }
        public BindingList<Account> Accounts { get { return accounts; } }
        public BindingList<Category> Categories { get { return categories; } }
        public List<Budget> Budgets { get { return budgets; } }

        public bool CanUndo { get { return undoStack.Count > 0; } }
        public bool CanRedo { get { return redoStack.Count > 0; } }

        // ---------- загрузка ----------

        private void Load(bool seedDemoIfEmpty)
        {
            categories = ToBindingList(data.Load<Category>("categories"));
            if (categories.Count == 0) SeedCategories();

            accounts = ToBindingList(data.Load<Account>("accounts"));
            if (accounts.Count == 0) SeedAccounts();

            transactions = ToBindingList(data.Load<Transaction>("transactions"));
            budgets = data.Load<Budget>("budgets") ?? new List<Budget>();

            if (transactions.Count > 0)
                nextTransactionId = transactions.Max(t => t.Id) + 1;

            // если операций нет — наполняем примерами (в тестах отключается)
            if (seedDemoIfEmpty && transactions.Count == 0)
            {
                GenerateDemoTransactions();
                Save();
            }
        }

        private static BindingList<T> ToBindingList<T>(List<T> source)
        {
            return source == null ? new BindingList<T>() : new BindingList<T>(source);
        }

        // данные при первом запуске
        private void SeedCategories()
        {
            categories.Add(new Category { Id = 1, Name = "Зарплата", Type = TransactionType.Income, Color = "#4CAF50" });
            categories.Add(new Category { Id = 2, Name = "Подработка", Type = TransactionType.Income, Color = "#8BC34A" });
            categories.Add(new Category { Id = 3, Name = "Продукты", Type = TransactionType.Expense, Color = "#FF7043" });
            categories.Add(new Category { Id = 4, Name = "Транспорт", Type = TransactionType.Expense, Color = "#42A5F5" });
            categories.Add(new Category { Id = 5, Name = "Развлечения", Type = TransactionType.Expense, Color = "#AB47BC" });
            categories.Add(new Category { Id = 6, Name = "Кафе", Type = TransactionType.Expense, Color = "#FFA726" });
            categories.Add(new Category { Id = 7, Name = "Здоровье", Type = TransactionType.Expense, Color = "#26C6DA" });
            categories.Add(new Category { Id = 8, Name = "Прочее", Type = TransactionType.Expense, Color = "#BDBDBD" });
        }

        private void SeedAccounts()
        {
            accounts.Add(new Account { Id = 1, Name = "Наличные", Balance = 12000, InitialBalance = 12000, AccountType = "Cash" });
            accounts.Add(new Account { Id = 2, Name = "Карта", Balance = 20000, InitialBalance = 20000, AccountType = "Card" });
            accounts.Add(new Account { Id = 3, Name = "Копилка", Balance = 60000, InitialBalance = 60000, AccountType = "Savings" });
        }

        // ---------- операции с транзакциями ----------

        public void AddTransaction(Transaction t)
        {
            PushUndo();
            AddRaw(t);
            Save();
            OnChanged();
            CheckBudget(t);
        }

        // добавление без записи в историю — используется внутри
        private void AddRaw(Transaction t)
        {
            t.Id = nextTransactionId++;
            t.CreatedAt = DateTime.Now;
            ApplyToBalance(t, add: true);
            transactions.Add(t);
        }

        public void UpdateTransaction(Transaction edited)
        {
            var old = transactions.FirstOrDefault(x => x.Id == edited.Id);
            if (old == null) return;

            PushUndo();

            // сначала откатываем старое влияние на баланс, потом применяем новое
            ApplyToBalance(old, add: false);

            old.Date = edited.Date;
            old.Amount = edited.Amount;
            old.Type = edited.Type;
            old.CategoryId = edited.CategoryId;
            old.AccountId = edited.AccountId;
            old.Description = edited.Description;

            ApplyToBalance(old, add: true);

            Save();
            OnChanged();
            CheckBudget(old);
        }

        public void DeleteTransaction(int id)
        {
            var t = transactions.FirstOrDefault(x => x.Id == id);
            if (t == null) return;

            PushUndo();
            ApplyToBalance(t, add: false);
            transactions.Remove(t);
            Save();
            OnChanged();
        }

        private void ApplyToBalance(Transaction t, bool add)
        {
            var acc = accounts.FirstOrDefault(a => a.Id == t.AccountId);
            if (acc == null) return;

            decimal sign = t.Type == TransactionType.Income ? 1 : -1;
            if (!add) sign = -sign;
            acc.Balance += sign * t.Amount;
        }

        // ---------- отмена / повтор ----------

        public void Undo()
        {
            if (undoStack.Count == 0) return;
            redoStack.Push(SerializeState());
            ApplyState(undoStack.Pop());
            Save();
            OnChanged();
        }

        public void Redo()
        {
            if (redoStack.Count == 0) return;
            undoStack.Push(SerializeState());
            ApplyState(redoStack.Pop());
            Save();
            OnChanged();
        }

        private void PushUndo()
        {
            undoStack.Push(SerializeState());
            redoStack.Clear();
        }

        private string SerializeState()
        {
            var snap = new StateSnapshot
            {
                Transactions = transactions.ToList(),
                Accounts = accounts.ToList(),
                Budgets = budgets.ToList(),
                NextId = nextTransactionId
            };
            return JsonConvert.SerializeObject(snap);
        }

        private void ApplyState(string json)
        {
            var snap = JsonConvert.DeserializeObject<StateSnapshot>(json);

            transactions.Clear();
            foreach (var t in snap.Transactions) transactions.Add(t);

            accounts.Clear();
            foreach (var a in snap.Accounts) accounts.Add(a);

            budgets.Clear();
            budgets.AddRange(snap.Budgets);

            nextTransactionId = snap.NextId;
        }

        private class StateSnapshot
        {
            public List<Transaction> Transactions { get; set; }
            public List<Account> Accounts { get; set; }
            public List<Budget> Budgets { get; set; }
            public int NextId { get; set; }
        }

        // ---------- демо-данные ----------

        public void GenerateDemoData()
        {
            PushUndo();
            // полностью пересоздаём демо-данные: свежие счета и операции,
            // чтобы повторные нажатия не накапливали суммы и баланс не уходил в минус
            transactions.Clear();
            accounts.Clear();
            SeedAccounts();
            nextTransactionId = 1;
            GenerateDemoTransactions();
            Save();
            OnChanged();
        }

        private void GenerateDemoTransactions()
        {
            if (categories.Count == 0 || accounts.Count == 0) return;

            var rnd = new Random(2024);
            var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            int card = AccByType("Card");
            int cash = AccByType("Cash");

            // полгода истории
            for (int back = 5; back >= 0; back--)
            {
                var month = thisMonth.AddMonths(-back);
                int days = DateTime.DaysInMonth(month.Year, month.Month);

                // зарплата
                AddRaw(Make(month.AddDays(4), 40000, TransactionType.Income, "Зарплата", card, "Зарплата"));

                // иногда небольшая подработка
                if (rnd.Next(0, 10) < 3)
                    AddRaw(Make(month.AddDays(rnd.Next(10, 20)), rnd.Next(4, 8) * 1000, TransactionType.Income, "Подработка", card, "Подработка"));

                // аренда и коммуналка — крупный фиксированный расход каждый месяц
                AddRaw(Make(month.AddDays(10), 20000, TransactionType.Expense, "Прочее", card, "Аренда и коммуналка"));

                // продукты — несколько раз за месяц
                int groceryTimes = rnd.Next(4, 7);
                for (int i = 0; i < groceryTimes; i++)
                    AddRaw(Make(month.AddDays(rnd.Next(1, days)), rnd.Next(8, 35) * 100, TransactionType.Expense, "Продукты", card, "Продукты"));

                // транспорт (немного и наличными — чтобы наличные не ушли в минус)
                for (int i = 0; i < rnd.Next(1, 3); i++)
                    AddRaw(Make(month.AddDays(rnd.Next(1, days)), rnd.Next(2, 7) * 100, TransactionType.Expense, "Транспорт", cash, "Проезд"));

                // кафе
                for (int i = 0; i < rnd.Next(1, 4); i++)
                    AddRaw(Make(month.AddDays(rnd.Next(1, days)), rnd.Next(4, 14) * 100, TransactionType.Expense, "Кафе", card, "Кофе/обед"));

                // развлечения
                if (rnd.Next(0, 2) == 1)
                    AddRaw(Make(month.AddDays(rnd.Next(1, days)), rnd.Next(10, 30) * 100, TransactionType.Expense, "Развлечения", card, "Кино/прогулка"));

                // здоровье изредка
                if (back % 2 == 0)
                    AddRaw(Make(month.AddDays(rnd.Next(1, days)), rnd.Next(8, 25) * 100, TransactionType.Expense, "Здоровье", card, "Аптека"));
            }
        }

        private Transaction Make(DateTime date, decimal amount, TransactionType type, string categoryName, int accountId, string desc)
        {
            return new Transaction
            {
                Date = date,
                Amount = amount,
                Type = type,
                CategoryId = CatByName(categoryName),
                AccountId = accountId,
                Description = desc
            };
        }

        private int CatByName(string name)
        {
            var c = categories.FirstOrDefault(x => x.Name == name);
            return c != null ? c.Id : categories.First().Id;
        }

        private int AccByType(string type)
        {
            var a = accounts.FirstOrDefault(x => x.AccountType == type);
            return a != null ? a.Id : accounts.First().Id;
        }

        // ---------- бюджеты ----------

        public void SetBudget(int categoryId, int month, int year, decimal limit)
        {
            PushUndo();
            var b = budgets.FirstOrDefault(x => x.CategoryId == categoryId && x.Month == month && x.Year == year);
            if (b == null)
            {
                b = new Budget { Id = budgets.Count + 1, CategoryId = categoryId, Month = month, Year = year };
                budgets.Add(b);
            }
            b.Limit = limit;
            Save();
            OnChanged();
        }

        public decimal SpentInCategory(int categoryId, int month, int year)
        {
            return transactions
                .Where(t => t.Type == TransactionType.Expense
                            && t.CategoryId == categoryId
                            && t.Date.Month == month && t.Date.Year == year)
                .Sum(t => t.Amount);
        }

        private void CheckBudget(Transaction t)
        {
            if (t.Type != TransactionType.Expense || BudgetExceeded == null) return;

            var b = budgets.FirstOrDefault(x => x.CategoryId == t.CategoryId
                                                && x.Month == t.Date.Month && x.Year == t.Date.Year);
            if (b == null || b.Limit <= 0) return;

            decimal spent = SpentInCategory(t.CategoryId, t.Date.Month, t.Date.Year);
            if (spent > b.Limit)
            {
                var cat = CategoryById(t.CategoryId);
                string name = cat != null ? cat.Name : "категория";
                BudgetExceeded(this, string.Format("Превышен лимит по «{0}»: {1:N0} из {2:N0} \u20BD",
                    name, spent, b.Limit));
            }
        }

        // ---------- статистика ----------

        public decimal TotalBalance()
        {
            return accounts.Sum(a => a.Balance);
        }

        public Dictionary<Category, decimal> ExpensesByCategory(DateTime from, DateTime to)
        {
            return transactions
                .Where(t => t.Type == TransactionType.Expense
                            && t.Date.Date >= from.Date && t.Date.Date <= to.Date)
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => CategoryById(g.Key) ?? Unknown(g.Key), g => g.Sum(t => t.Amount));
        }

        public void MonthlyTotals(int month, int year, out decimal income, out decimal expense)
        {
            var inMonth = transactions.Where(t => t.Date.Month == month && t.Date.Year == year).ToList();
            income = inMonth.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            expense = inMonth.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        }

        public List<Transaction> InPeriod(DateTime from, DateTime to)
        {
            return transactions
                .Where(t => t.Date.Date >= from.Date && t.Date.Date <= to.Date)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        public Category CategoryById(int id)
        {
            return categories.FirstOrDefault(c => c.Id == id);
        }

        public Account AccountById(int id)
        {
            return accounts.FirstOrDefault(a => a.Id == id);
        }

        private static Category Unknown(int id)
        {
            return new Category { Id = id, Name = "(без категории)", Color = "#9E9E9E" };
        }

        // ---------- сохранение ----------

        public void Save()
        {
            data.Save("transactions", transactions.ToList());
            data.Save("accounts", accounts.ToList());
            data.Save("categories", categories.ToList());
            data.Save("budgets", budgets);
        }

        private void OnChanged()
        {
            if (Changed != null) Changed(this, EventArgs.Empty);
        }
    }
}
