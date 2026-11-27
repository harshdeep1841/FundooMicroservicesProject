using Fundoo.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<PasswordResetToken> PasswordResetTokens =>
        Set<PasswordResetToken>();

    public DbSet<ExternalLogin> ExternalLogins =>
        Set<ExternalLogin>();

    public DbSet<UserSession> UserSessions =>
        Set<UserSession>();

    public DbSet<RefreshOperation> RefreshOperations =>
        Set<RefreshOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                .IsRequired();
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.TokenHash)
                .IsRequired();

            entity.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new
                {
                    e.Provider,
                    e.ProviderUserId
                })
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(session => session.Id);

            entity.Property(session => session.DeviceName)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(session => session.RefreshTokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(session => session.PreviousRefreshTokenHash)
                .HasMaxLength(64);

            entity.Property(session => session.Version)
                .IsConcurrencyToken();

            entity.HasIndex(session => session.RefreshTokenHash)
                .IsUnique();

            entity.HasIndex(session => session.PreviousRefreshTokenHash);

            entity.HasIndex(session => new
            {
                session.UserId,
                session.RevokedAtUtc,
                session.ExpiresAtUtc
            });

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshOperation>(entity =>
        {
            entity.HasKey(operation => operation.Id);

            entity.Property(operation => operation.PresentedTokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(operation => operation.EncryptedResponse)
                .IsRequired();

            entity.HasIndex(operation => new
            {
                operation.SessionId,
                operation.PresentedTokenHash
            })
            .IsUnique();

            entity.HasIndex(operation => operation.ExpiresAtUtc);

            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(operation => operation.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
