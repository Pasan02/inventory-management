using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_management.Data.Entities
{
    [Table("user_login_audits")]
    public class UserLoginAudit
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_account_id")]
        public int? UserAccountId { get; set; }

        [ForeignKey(nameof(UserAccountId))]
        public UserAccount? UserAccount { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column("machine_name")]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Column("success")]
        public bool Success { get; set; }

        [Column("failure_reason")]
        [MaxLength(200)]
        public string? FailureReason { get; set; }
    }
}
