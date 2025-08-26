using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class BudgetRules
    {
        public long BudgetRulesID { get; set; }
        public long UserID { get; set; }
        
        [Required]
        [Range(0, 100, ErrorMessage = "Savings percentage must be between 0 and 100")]
        public int Savings { get; set; }
        
        [Required]
        [Range(0, 100, ErrorMessage = "Needs percentage must be between 0 and 100")]
        public int Needs { get; set; }
        
        [Required]
        [Range(0, 100, ErrorMessage = "Wants percentage must be between 0 and 100")]
        public int Wants { get; set; }
        
        public DateTime? updated_at { get; set; }
    }
}
