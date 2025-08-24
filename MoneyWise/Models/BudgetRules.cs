using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class BudgetRules
    {
        public int BudgetRulesID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public int TotalAmount { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Savings amount must be greater than or equal to 0")]
        public int Savings { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Needs amount must be greater than or equal to 0")]
        public int Needs { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Wants amount must be greater than or equal to 0")]
        public int Wants { get; set; }
        
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
