using System;
using System.Drawing;
using Newtonsoft.Json;

namespace FinanceTracker.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public int CategoryId { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // как показывать сумму в таблице: +/- перед числом
        [JsonIgnore]
        public string DisplayAmount
        {
            get
            {
                return Type == TransactionType.Income
                    ? "+" + Amount.ToString("N2")
                    : "-" + Amount.ToString("N2");
            }
        }

        [JsonIgnore]
        public Color AmountColor
        {
            get { return Type == TransactionType.Income ? Color.SeaGreen : Color.Firebrick; }
        }
    }
}
