using HighConvertAI.Server.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace HighConvertAI.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ── Existing ─────────────────────────────────────────────────────────
        public DbSet<PageScan> PageScans => Set<PageScan>();
        public DbSet<PageScanDetail> PageScanDetails => Set<PageScanDetail>();
        public DbSet<IssueGroup> IssueGroups => Set<IssueGroup>();
        public DbSet<IssueSeverity> IssueSeveritys => Set<IssueSeverity>();
        public DbSet<Issue> Issues => Set<Issue>();


        // ── Auth ─────────────────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ExternalLogin> ExternalLogins { get; set; }

        // ── Model Building ────────────────────────────────────────────────────
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Auth
            ConfigureUser(modelBuilder);
            ConfigureRole(modelBuilder);
            ConfigureUserRole(modelBuilder);
            ConfigureOtpCode(modelBuilder);
            ConfigureRefreshToken(modelBuilder);
            ConfigureExternalLogin(modelBuilder);
        }

        // ── Auth Configuration ────────────────────────────────────────────────

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                // Nullable — social login users have no password
                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(512);

                entity.Property(x => x.AvatarUrl)
                    .HasMaxLength(500);

                entity.Property(x => x.IsEmailVerified)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(x => x.Email)
                    .IsUnique();
            });
        }

        private static void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.Description)
                    .HasMaxLength(255);

                entity.HasIndex(x => x.Name)
                    .IsUnique();

                // Seed default roles
                entity.HasData(
                    new Role { Id = 1, Name = "Admin", Description = "Full system access" },
                    new Role { Id = 2, Name = "User", Description = "Can perform audits" }
                );
            });
        }

        private static void ConfigureUserRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");

                // Composite primary key — no surrogate Id needed
                entity.HasKey(x => new { x.UserId, x.RoleId });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);   // Delete user → remove their role links

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);  // Cannot delete a role that is in use
            });
        }

        private static void ConfigureOtpCode(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.ToTable("OtpCodes");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                // Always store the hash, never the raw OTP
                entity.Property(x => x.CodeHash)
                    .IsRequired()
                    .HasMaxLength(512);

                // "REGISTER" | "PASSWORD_RESET"
                entity.Property(x => x.Purpose)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(x => x.IsUsed)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x => x.AttemptCount)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Fast lookup when validating OTP submissions
                entity.HasIndex(x => new { x.Email, x.Purpose });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.OtpCodes)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");

                entity.HasKey(x => x.Id);

                // Store only the SHA-256 hash of the token sent via HttpOnly cookie
                entity.Property(x => x.TokenHash)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(x => x.ExpiresAt)
                    .IsRequired();

                entity.Property(x => x.IsRevoked)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Tracks rotation chain for breach detection
                entity.Property(x => x.ReplacedByTokenId)
                    .IsRequired(false);

                entity.Property(x => x.DeviceInfo)
                    .HasMaxLength(255);

                entity.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(x => x.TokenHash)
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureExternalLogin(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.ToTable("ExternalLogins");

                entity.HasKey(x => x.Id);

                // "Google" | "Facebook"
                entity.Property(x => x.Provider)
                    .IsRequired()
                    .HasMaxLength(50);

                // The unique ID issued by the external provider (e.g. Google's "sub" claim)
                entity.Property(x => x.ProviderUserId)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(x => x.ProviderEmail)
                    .HasMaxLength(255);

                entity.Property(x => x.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // One provider account can only link to one local user
                entity.HasIndex(x => new { x.Provider, x.ProviderUserId })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.ExternalLogins)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}