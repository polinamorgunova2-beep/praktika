namespace FinanceTracker.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TransactionType Type { get; set; }
        public string Color { get; set; }   // hex-цвет для диаграмм, напр. "#FF7043"

        public override string ToString()
        {
            return Name;
        }
    }
}
