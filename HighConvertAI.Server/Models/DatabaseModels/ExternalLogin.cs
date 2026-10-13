using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    /// <summary>
    /// Known OAuth providers. Add more as needed.
    /// </summary>
    public static class ExternalProvider
    {
        public const string Google = "Google";
        public const string Facebook = "Facebook";
    }

    /// <summary>
    /// Links a local User account to one or more external OAuth identities.
    /// One user can have both a Google and a Facebook login.
    /// </summary>
    public class ExternalLogin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public Guid UserId { get; set; }

        /// <summary>
        /// ExternalProvider constant: "Google" | "Facebook"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// The unique user ID issued by the external provider (sub claim for Google).
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ProviderUserId { get; set; } = string.Empty;

        /// <summary>
        /// Email returned by the provider at the time of linking.
        /// May differ from User.Email if the user signed up with a different address.
        /// </summary>
        [MaxLength(255)]
        public string? ProviderEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ──────────────────────────────────────────────────────────

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}
