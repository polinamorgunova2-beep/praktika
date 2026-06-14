using Newtonsoft.Json;

namespace FinanceTracker.Models
{
    // Лимит расходов по категории на конкретный месяц.
    public class Budget
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Limit { get; set; }

        // фактически потрачено — считается контроллером, в файл не сохраняем
        [JsonIgnore]
        public decimal Spent { get; set; }

        [JsonIgnore]
        public decimal Remaining
        {
            get { return Limit - Spent; }
        }

        [JsonIgnore]
        public int PercentUsed
        {
            get { return Limit > 0 ? (int)(Spent / Limit * 100) : 0; }
        }
    }
}
