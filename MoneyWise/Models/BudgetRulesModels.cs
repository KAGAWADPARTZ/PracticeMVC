using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MoneyWise.Models
{
    public class BudgetRules
    {
        public int BudgetRulesID { get; set; } 
        public int UserID { get; set; }
        public int SavingsAmount { get; set; }
        public int WantsAmount { get; set; }
        public int NeedsAmount { get; set; }
        public int TotalAmount { get; set; }
        public DateTime? updated_at { get; set; }
    }
}