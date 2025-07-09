namespace MoneyWise.Models
{
    public class Transaction
    {
        public required int  TransactionID { get; set; }
        public required int UserID { get; set; }
        public required string Description { get; set; }
        public string? Type { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
    }
}
