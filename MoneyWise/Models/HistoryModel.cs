using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class HistoryModel
    {
        public int HistoryID { get; set; }
        public DateTime? created_at { get; set; }
        public int UserID { get; set; }
        public string? Type { get; set; }
        public int Amount { get; set; } 
    }

    public class HistoryRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        
        public int Amount { get; set; }
    }
}
