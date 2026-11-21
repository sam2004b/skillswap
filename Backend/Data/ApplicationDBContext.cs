using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext : IdentityDbContext<AuthenticationUser>
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

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<StripeWebhookEvent> StripeWebhookEvents =>
        Set<StripeWebhookEvent>();

    public DbSet<TaxProfile> TaxProfiles => Set<TaxProfile>();

    public DbSet<TaxRecord> TaxRecords => Set<TaxRecord>();

    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();

    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();

    public DbSet<SkillSwapRequest> SkillSwapRequests => Set<SkillSwapRequest>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Availability>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .Property(b => b.PaymentMethod).HasDefaultValue("Money");

        builder.Entity<Skill>()
            .Property(s => s.CreditRate).HasDefaultValue(10m);

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

        builder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.Student)
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.Teacher)
            .WithMany()
            .HasForeignKey(p => p.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasIndex(p => p.BookingId)
            .IsUnique();

        builder.Entity<Payment>()
            .HasIndex(p => p.StudentId);

        builder.Entity<Payment>()
            .HasIndex(p => p.TeacherId);

        builder.Entity<Payment>()
            .HasIndex(p => p.StripeCheckoutSessionId)
            .IsUnique()
            .HasFilter("\"StripeCheckoutSessionId\" IS NOT NULL");

        builder.Entity<Payment>()
            .HasIndex(p => p.StripePaymentIntentId)
            .IsUnique()
            .HasFilter("\"StripePaymentIntentId\" IS NOT NULL");

        builder.Entity<StripeWebhookEvent>()
            .HasIndex(e => e.StripeEventId)
            .IsUnique();

        builder.Entity<AuthenticationUser>()
            .HasIndex(u => u.StripeAccountId)
            .IsUnique()
            .HasFilter("\"StripeAccountId\" IS NOT NULL");

        builder.Entity<TaxProfile>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaxProfile>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<TaxRecord>()
            .HasOne(t => t.Payment)
            .WithOne()
            .HasForeignKey<TaxRecord>(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaxRecord>()
            .HasIndex(t => t.PaymentId)
            .IsUnique();

        builder.Entity<CreditWallet>()
            .HasOne(w => w.User).WithMany()
            .HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CreditWallet>().HasIndex(w => w.UserId).IsUnique();

        builder.Entity<CreditTransaction>()
            .HasOne(t => t.Wallet).WithMany()
            .HasForeignKey(t => t.UserId).HasPrincipalKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CreditTransaction>().HasIndex(t => t.Reference).IsUnique();
        builder.Entity<CreditTransaction>().HasIndex(t => new { t.UserId, t.CreatedAtUtc });

        builder.Entity<SkillSwapRequest>()
            .HasOne(s => s.Proposer).WithMany().HasForeignKey(s => s.ProposerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SkillSwapRequest>()
            .HasOne(s => s.Recipient).WithMany().HasForeignKey(s => s.RecipientId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SkillSwapRequest>()
            .HasOne(s => s.OfferedSkill).WithMany().HasForeignKey(s => s.OfferedSkillId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SkillSwapRequest>()
            .HasOne(s => s.RequestedSkill).WithMany().HasForeignKey(s => s.RequestedSkillId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SkillSwapRequest>().HasIndex(s => new { s.RecipientId, s.Status });

        builder.Entity<UserNotification>()
            .HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserNotification>().HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAtUtc });
    }
}
