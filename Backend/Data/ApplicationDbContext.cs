
using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Booking relationships
        builder.Entity<Booking>()
            .HasOne(b => b.Learner)
            .WithMany()
            .HasForeignKey(b => b.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Teacher)
            .WithMany()
            .HasForeignKey(b => b.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Skill)
            .WithMany()
            .HasForeignKey(b => b.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking indexes
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
                b.LearnerId,
                b.StartAtUtc,
                b.EndAtUtc
            });

        // Review relationship with Booking
        builder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review relationship with reviewer
        builder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review relationship with teacher
        builder.Entity<Review>()
            .HasOne(r => r.Teacher)
            .WithMany()
            .HasForeignKey(r => r.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Only one review per booking
        builder.Entity<Review>()
            .HasIndex(r => r.BookingId)
            .IsUnique();
    }
}
