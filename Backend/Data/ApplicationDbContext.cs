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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        builder.Entity<Booking>()
            .HasIndex(b => new { b.TeacherId, b.StartAtUtc, b.EndAtUtc });

        builder.Entity<Booking>()
            .HasIndex(b => new { b.LearnerId, b.StartAtUtc, b.EndAtUtc });
    }
}
