using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [MaxLength(512)]
        public string? PasswordHash { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsActive { get; set; } = false; 
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ──────────────────────────────────────────────────────────

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

        public ICollection<PageScan> PageScans { get; set; } = new List<PageScan>();

    }
}
