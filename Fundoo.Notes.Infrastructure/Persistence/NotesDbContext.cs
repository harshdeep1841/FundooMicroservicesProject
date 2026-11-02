using Fundoo.Notes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.Notes.Infrastructure.Persistence;

public class NotesDbContext : DbContext
{
    public NotesDbContext(
        DbContextOptions<NotesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();
    
    public DbSet<Label> Labels => Set<Label>();

    public DbSet<NoteLabel> NoteLabels => Set<NoteLabel>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(n => n.Content)
                .IsRequired();

            entity.Property(n => n.UserId)
                .IsRequired();

            entity.HasIndex(n => n.UserId);
        });
        
        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(l => l.UserId)
                .IsRequired();

            entity.HasIndex(l => new { l.UserId, l.Name })
                .IsUnique();
        });

        modelBuilder.Entity<NoteLabel>(entity =>
        {
            entity.HasKey(nl => new { nl.NoteId, nl.LabelId });

            entity.HasOne(nl => nl.Note)
                .WithMany(n => n.NoteLabels)
                .HasForeignKey(nl => nl.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(nl => nl.Label)
                .WithMany(l => l.NoteLabels)
                .HasForeignKey(nl => nl.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}