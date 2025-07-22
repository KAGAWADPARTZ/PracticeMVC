using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyWise.Models
{
    [Table("Users")]
    public class Users
    {
        [Key]
        public int UserID { get; set; }

       // [Required]
        public string? Username { get; set; }

       // [Required]
        public string? Email { get; set; }

        public DateTime? created_at { get; set; }
    }
}