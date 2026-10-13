using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public static class OtpPurpose
    {
        public const string Register = "REGISTER";
        public const string PasswordReset = "PASSWORD_RESET";
    }

    public class OtpCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hash of the 6-digit OTP — never store plain text.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string CodeHash { get; set; } = string.Empty;

        /// <summary>
        /// OtpPurpose constant: "REGISTER" | "PASSWORD_RESET"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Tracks failed attempts. Invalidate after 3 wrong tries.
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ──────────────────────────────────────────────────────────

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
