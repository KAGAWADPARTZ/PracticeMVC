namespace MoneyWise.Models
{
    public class Users
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        
        public string? Type { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
    }
    //transfer the login here from the userRepository
}
