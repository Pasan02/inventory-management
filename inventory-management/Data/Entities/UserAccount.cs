using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace inventory_management.Data.Entities
{
    [Table("user_accounts")]
    [Index(nameof(Username), IsUnique = true)]
    public class UserAccount
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        [MaxLength(128)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("password_salt")]
        [MaxLength(128)]
        public string PasswordSalt { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_utc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
