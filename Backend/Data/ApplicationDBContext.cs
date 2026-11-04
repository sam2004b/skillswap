using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Skill> Skills { get; set; }

    public DbSet<Availability> Availabilities => Set<Availability>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Booking>()
            .HasOne(b => b.Skill)
            .WithMany()
            .HasForeignKey(b => b.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Student)
            .WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Teacher)
            .WithMany()
            .HasForeignKey(b => b.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasIndex(b => new
            {
                b.TeacherId,
                b.StartAtUtc,
                b.EndAtUtc
            });

        builder.Entity<Booking>()
            .HasIndex(b => new
            {
                b.StudentId,
                b.StartAtUtc,
                b.EndAtUtc
            });

        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasIndex(m => new
            {
                m.SenderId,
                m.ReceiverId,
                m.SentAtUtc
            });

        builder.Entity<Message>()
            .HasIndex(m => new
            {
                m.ReceiverId,
                m.ReadAtUtc
            });

        builder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Teacher)
            .WithMany()
            .HasForeignKey(r => r.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasIndex(r => r.BookingId)
            .IsUnique();

        builder.Entity<Review>()
            .HasIndex(r => r.TeacherId);
    }
}