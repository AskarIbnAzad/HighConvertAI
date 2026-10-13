using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public Guid UserId { get; set; }

        /// <summary>
        /// SHA-256 hash of the raw token sent to the client via HttpOnly cookie.
        /// Never store the raw token.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// When a rotated token is used a second time it signals a breach.
        /// Store the replacement token's ID so the whole chain can be revoked.
        /// </summary>
        public long? ReplacedByTokenId { get; set; }

        /// <summary>
        /// Optional: browser / device fingerprint for anomaly detection.
        /// </summary>
        [MaxLength(255)]
        public string? DeviceInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Computed helpers ─────────────────────────────────────────────────────

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;

        // ── Navigation ──────────────────────────────────────────────────────────

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
