using Microsoft.EntityFrameworkCore;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<GroupLessonSlot> GroupLessonSlots { get; set; }
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
        public DbSet<TelegramChatBinding> TelegramChatBindings { get; set; }
        public DbSet<TelegramMessageDelivery> TelegramMessageDeliveries { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<TrainerWorkingHour> TrainerWorkingHours { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasFilter("\"Email\" IS NOT NULL");

                entity.HasIndex(e => e.Phone)
                    .IsUnique()
                    .HasFilter("\"Phone\" IS NOT NULL");

                entity.HasIndex(e => e.TelegramChatId)
                    .IsUnique()
                    .HasFilter("\"TelegramChatId\" IS NOT NULL");

                entity.HasIndex(e => e.Role);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.TelegramUsername)
                    .HasMaxLength(50);

                entity.Property(e => e.TelegramChatId)
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Client");

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RefreshToken)
                    .HasMaxLength(500);

                entity.Property(e => e.HasLateCancellationPenalty)
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Trainer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<Trainer>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Bio).HasMaxLength(1000);
                entity.Property(e => e.PhotoUrl).HasMaxLength(500);
                entity.Property(e => e.Instagram).HasMaxLength(100);

                entity.Property(e => e.DanceStyles)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .HasColumnName("DanceStyles")
                    .HasColumnType("text");
            });

            modelBuilder.Entity<TrainerWorkingHour>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.WorkingHours)
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TrainerId, e.DayOfWeek, e.StartTimeLocal, e.EndTimeLocal })
                    .IsUnique();
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasPrecision(10, 2);
                entity.Property(e => e.Format).IsRequired().HasMaxLength(20).HasDefaultValue("Individual");
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Format).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Price).HasPrecision(10, 2);

                entity.HasOne(e => e.SourceService)
                    .WithMany()
                    .HasForeignKey(e => e.SourceServiceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.Bookings)
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Service)
                    .WithMany()
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.GroupLessonSlot)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(e => e.GroupLessonSlotId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.UserId, e.SubscriptionId });

                entity.Property(e => e.PriceAtBooking)
                    .HasPrecision(10, 2);

                entity.Property(e => e.PaymentMode)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Single");
            });

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SubscriptionPlan)
                    .WithMany(p => p.Subscriptions)
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.Status });
                entity.HasIndex(e => e.PaymentDeadlineAt);
            });

            modelBuilder.Entity<GroupLessonSlot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ServiceId, e.TrainerId, e.StartTime }).IsUnique();

                entity.HasOne(e => e.Service)
                    .WithMany()
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Trainer)
                    .WithMany()
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ScheduleSlot>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.ScheduleSlots)
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Booking)
                    .WithMany(b => b.ScheduleSlots)
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.TrainerId, e.StartTime }).IsUnique();
                entity.HasIndex(e => new { e.TrainerId, e.StartTime, e.IsBooked });
            });

            modelBuilder.Entity<TelegramChatBinding>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.TelegramChatBindings)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");

                entity.HasIndex(e => e.TelegramChatId)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");

                entity.Property(e => e.TelegramChatId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TelegramUsername)
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<TelegramMessageDelivery>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.TelegramMessageDeliveries)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.Status, e.CreatedAt });
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.RelatedEntityId);

                entity.Property(e => e.RecipientChatId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.RecipientLogValue)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.MessageType)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.ErrorMessage)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.PasswordResetTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.ExpiresAt });

                entity.Property(e => e.TokenHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });
        }
    }
}
