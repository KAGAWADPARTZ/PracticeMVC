using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MoneyWise.Models
{
    public class Users
    {
        [Key]
        [Column("UserID")]
        [JsonPropertyName("UserID")]  // Add this to ensure proper JSON mapping
        public int UserID { get; set; }
        
        [JsonPropertyName("Username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("Email")]
        public string? Email { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime? created_at { get; set; }
    }
}