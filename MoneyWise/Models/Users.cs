namespace MoneyWise.Models
{
    public class Users
    {
        public int UserID { get; set; }
        public  string? Username { get; set; }
        public string? Email { get; set; }
        public string? created_at { get; set; }
    }
    //transfer the login here from the userRepository
}
