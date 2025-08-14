using System.ComponentModel.DataAnnotations;

namespace MoneyWise.Models
{
    public class HistoryModel
    {
        public int HistoryID { get; set; }
        public int UserID { get; set; }
        public string Type { get; set; } = string.Empty; // "deposit" or "withdrawal"
        public float[] Amount { get; set; } = new float[0]; // Array to store amount values
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class HistoryRequest
    {
        public float Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
