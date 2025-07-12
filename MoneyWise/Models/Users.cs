namespace MoneyWise.Models
{
    public class Users
    {
        public int UserID { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? created_at { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
    }

}
