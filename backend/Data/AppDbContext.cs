using DersNotlari.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DersNotlari.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.Property(note => note.CourseName).HasMaxLength(160).IsRequired();
            entity.Property(note => note.Description).HasMaxLength(2000).IsRequired();
            entity.Property(note => note.FileName).HasMaxLength(260);
            entity.Property(note => note.FilePath).HasMaxLength(500);
            entity.Property(note => note.ContentType).HasMaxLength(120);
            entity.HasOne(note => note.AppUser)
                .WithMany(user => user.Notes)
                .HasForeignKey(note => note.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
