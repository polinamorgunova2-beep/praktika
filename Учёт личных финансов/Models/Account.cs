namespace FinanceTracker.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public decimal InitialBalance { get; set; }
        public string AccountType { get; set; }   // Cash / Card / Savings

        public override string ToString()
        {
            return Name + " (" + Balance.ToString("N0") + " \u20BD)";
        }
    }
}
