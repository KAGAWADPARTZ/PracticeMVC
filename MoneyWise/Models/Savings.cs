using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class Savings
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }  // Changed from Guid to int
        public decimal Amount { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class SavingsRequest
    {
        public decimal SavingsAmount { get; set; }
        public string Action { get; set; } = string.Empty; // "deposit" or "withdraw"
    }
}