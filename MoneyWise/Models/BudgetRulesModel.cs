
namespace MoneyWise.Models
{
    public class BudgetRulesModel
    {
        public long UserID { get; set; } 
        public int Savings { get; set; }  // This will store percentage (e.g., 50)
        public int Needs { get; set; }    // This will store percentage (e.g., 30)
        public int Wants { get; set; }    // This will store percentage (e.g., 20)
        public DateTime? updated_at { get; set; }
    }
}