using System.Drawing;
using NUnit.Framework;
using FinanceTracker.Models;

namespace FinanceTracker.Tests
{
    // Прямые юнит-тесты для классов моделей (без контроллера).
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void Budget_Remaining_IsLimitMinusSpent()
        {
            var b = new Budget { Limit = 5000, Spent = 1800 };
            Assert.AreEqual(3200, b.Remaining);
        }

        [Test]
        public void Budget_PercentUsed_CalculatedCorrectly()
        {
            var b = new Budget { Limit = 2000, Spent = 500 };
            Assert.AreEqual(25, b.PercentUsed);
        }

        [Test]
        public void Budget_PercentUsed_IsZero_WhenNoLimit()
        {
            var b = new Budget { Limit = 0, Spent = 500 };
            Assert.AreEqual(0, b.PercentUsed);
        }

        [Test]
        public void Transaction_DisplayAmount_HasPlusForIncome()
        {
            var t = new Transaction { Type = TransactionType.Income, Amount = 1000 };
            StringAssert.StartsWith("+", t.DisplayAmount);
        }

        [Test]
        public void Transaction_DisplayAmount_HasMinusForExpense()
        {
            var t = new Transaction { Type = TransactionType.Expense, Amount = 1000 };
            StringAssert.StartsWith("-", t.DisplayAmount);
        }

        [Test]
        public void Transaction_AmountColor_DependsOnType()
        {
            var income = new Transaction { Type = TransactionType.Income };
            var expense = new Transaction { Type = TransactionType.Expense };
            Assert.AreEqual(Color.SeaGreen, income.AmountColor);
            Assert.AreEqual(Color.Firebrick, expense.AmountColor);
        }

        [Test]
        public void Category_ToString_ReturnsName()
        {
            var c = new Category { Name = "Продукты" };
            Assert.AreEqual("Продукты", c.ToString());
        }

        [Test]
        public void Account_ToString_ContainsName()
        {
            var a = new Account { Name = "Карта", Balance = 30000 };
            StringAssert.Contains("Карта", a.ToString());
        }
    }
}
