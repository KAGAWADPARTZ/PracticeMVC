
namespace MoneyWise.Models
{
    public class BudgetRulesModel
    {
        public int UserID { get; set; } 
        public int Savings { get; set; }  
        public int Needs { get; set; }
        public int Wants { get; set; }
        public int Amount { get; set; }
        public DateTime? updated_at { get; set; }
    }

}