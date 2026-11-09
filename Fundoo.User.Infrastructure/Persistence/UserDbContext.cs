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
    }
    
    
    
}