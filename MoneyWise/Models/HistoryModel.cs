using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MoneyWise.Models
{
    public class HistoryModel
    {
        [JsonIgnore]
        public int HistoryID { get; set; }
        public int UserID { get; set; }
        public string Type { get; set; } = string.Empty; // "deposit", "withdrawal", etc.
        public decimal Amount { get; set; }
        public DateTime? created_at { get; set; }
    }

    public class HistoryRequest
    {
        public int UserID { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
