using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyWise.Models
{
   
    public class Users
    {
        public Guid UserID { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public DateTime? created_at { get; set; }
    }
}