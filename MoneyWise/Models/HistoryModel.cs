using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class HistoryModel
    {
        public int HistoryID { get; set; }
        public DateTime? created_at { get; set; }
        public int UserID { get; set; }
        public string? Type { get; set; }
        public decimal Amount { get; set; } // Changed from float to decimal to match transaction amounts
    }

    public class HistoryRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; } // Changed from float to decimal
    }
}
