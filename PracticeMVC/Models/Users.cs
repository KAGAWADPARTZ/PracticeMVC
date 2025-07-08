namespace PracticeMVC.Models
{
    public class Users
    {
        public required int UserID { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? created_at { get; set; }
        public int? ContactNumber { get; set; }
        public string? Address { get; set; }
    }

}
