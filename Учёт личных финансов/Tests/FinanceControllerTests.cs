using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using FinanceTracker.Controllers;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.Tests
{
    [TestFixture]
    public class FinanceControllerTests
    {
        private string tempDir;
        private FinanceController controller;

        [SetUp]
        public void Setup()
        {
            // отдельная временная папка под каждый тест — данные не пересекаются.
            // второй параметр false: не создавать демо-данные, чтобы тесты были чистыми.
            tempDir = Path.Combine(Path.GetTempPath(), "ft_test_" + Guid.NewGuid().ToString("N"));
            controller = new FinanceController(new DataService(tempDir), false);
        }

        [TearDown]
        public void Cleanup()
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
            catch { /* не критично для теста */ }
        }

        private int ExpenseCategoryId()
        {
            return controller.Categories.First(c => c.Type == TransactionType.Expense).Id;
        }

        private int IncomeCategoryId()
        {
            return controller.Categories.First(c => c.Type == TransactionType.Income).Id;
        }

        [Test]
        public void AddExpense_DecreasesAccountBalance()
        {
            var acc = controller.Accounts.First();
            decimal before = acc.Balance;

            controller.AddTransaction(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 1500,
                AccountId = acc.Id,
                CategoryId = ExpenseCategoryId(),
                Date = DateTime.Today
            });

            Assert.AreEqual(before - 1500, acc.Balance);
        }

        [Test]
        public void AddIncome_IncreasesAccountBalance()
        {
            var acc = controller.Accounts.First();
            decimal before = acc.Balance;

            controller.AddTransaction(new Transaction
            {
                Type = TransactionType.Income,
                Amount = 20000,
                AccountId = acc.Id,
                CategoryId = IncomeCategoryId(),
                Date = DateTime.Today
            });

            Assert.AreEqual(before + 20000, acc.Balance);
        }

        [Test]
        public void DeleteTransaction_RestoresBalance()
        {
            var acc = controller.Accounts.First();
            decimal before = acc.Balance;

            controller.AddTransaction(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 800,
                AccountId = acc.Id,
                CategoryId = ExpenseCategoryId(),
                Date = DateTime.Today
            });

            int id = controller.Transactions.Last().Id;
            controller.DeleteTransaction(id);

            Assert.AreEqual(before, acc.Balance);
            Assert.IsFalse(controller.Transactions.Any(t => t.Id == id));
        }

        [Test]
        public void UpdateTransaction_AdjustsBalance()
        {
            var acc = controller.Accounts.First();
            decimal before = acc.Balance;
            int catId = ExpenseCategoryId();

            controller.AddTransaction(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 1000,
                AccountId = acc.Id,
                CategoryId = catId,
                Date = DateTime.Today
            });

            int id = controller.Transactions.Last().Id;
            controller.UpdateTransaction(new Transaction
            {
                Id = id,
                Type = TransactionType.Expense,
                Amount = 400,
                AccountId = acc.Id,
                CategoryId = catId,
                Date = DateTime.Today
            });

            // было списано 1000, после изменения должно быть списано только 400
            Assert.AreEqual(before - 400, acc.Balance);
        }

        [Test]
        public void SpentInCategory_SumsOnlyMatchingExpenses()
        {
            var acc = controller.Accounts.First();
            int catId = ExpenseCategoryId();
            var now = DateTime.Today;

            controller.AddTransaction(new Transaction { Type = TransactionType.Expense, Amount = 300, AccountId = acc.Id, CategoryId = catId, Date = now });
            controller.AddTransaction(new Transaction { Type = TransactionType.Expense, Amount = 700, AccountId = acc.Id, CategoryId = catId, Date = now });

            Assert.AreEqual(1000, controller.SpentInCategory(catId, now.Month, now.Year));
        }

        [Test]
        public void TotalBalance_EqualsSumOfAccounts()
        {
            decimal expected = controller.Accounts.Sum(a => a.Balance);
            Assert.AreEqual(expected, controller.TotalBalance());
        }

        [Test]
        public void Undo_RevertsLastAddedTransaction()
        {
            var acc = controller.Accounts.First();
            decimal before = acc.Balance;
            int countBefore = controller.Transactions.Count;

            controller.AddTransaction(new Transaction
            {
                Type = TransactionType.Expense,
                Amount = 500,
                AccountId = acc.Id,
                CategoryId = ExpenseCategoryId(),
                Date = DateTime.Today
            });

            controller.Undo();

            Assert.AreEqual(before, acc.Balance);
            Assert.AreEqual(countBefore, controller.Transactions.Count);
        }

        [Test]
        public void Budget_Persists_AfterReopen()
        {
            int catId = ExpenseCategoryId();
            var now = DateTime.Today;
            controller.SetBudget(catId, now.Month, now.Year, 5000);

            // новый контроллер на той же папке — лимит должен подтянуться из файла
            var reopened = new FinanceController(new DataService(tempDir), false);
            var b = reopened.Budgets.FirstOrDefault(x => x.CategoryId == catId && x.Month == now.Month && x.Year == now.Year);

            Assert.IsNotNull(b);
            Assert.AreEqual(5000, b.Limit);
        }
    }
}
