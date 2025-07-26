namespace MoneyWise.Models
{
    // backend database model
    public class Transaction
    {
        public int  TransactionID { get; set; }
        public required int UserID { get; set; }
        public float Amount { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    // frotend display model
     public class TransactionRequest
    {
        public float Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
