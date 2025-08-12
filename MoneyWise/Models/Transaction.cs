namespace MoneyWise.Models
{
    // backend database model
    public class Transaction
    {
        public int TransactionID { get; set; }
        public required int UserID { get; set; }
        public float Amount { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? TransactionType { get; set; } // "income", "expense", "transfer"
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    // frontend display model
    public class TransactionRequest
    {
        public float Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? TransactionType { get; set; }
    }

    // History filter model
    public class TransactionHistoryFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public float? MinAmount { get; set; }
        public float? MaxAmount { get; set; }
        public string? Category { get; set; }
        public string? TransactionType { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "created_at";
        public string SortOrder { get; set; } = "desc";
    }

    // Transaction history response model
    public class TransactionHistoryResponse
    {
        public List<Transaction> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public TransactionStatistics Statistics { get; set; } = new();
    }

    // Transaction statistics model
    public class TransactionStatistics
    {
        public float TotalIncome { get; set; }
        public float TotalExpenses { get; set; }
        public float NetAmount { get; set; }
        public int TotalTransactions { get; set; }
        public Dictionary<string, float> CategoryTotals { get; set; } = new();
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public float AverageTransactionAmount { get; set; }
        public float LargestTransaction { get; set; }
        public float SmallestTransaction { get; set; }
    }
}
