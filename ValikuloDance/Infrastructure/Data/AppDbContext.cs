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
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Индексы с правильным синтаксисом для PostgreSQL
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasFilter("\"Email\" IS NOT NULL");

                entity.HasIndex(e => e.Phone).IsUnique();

                entity.HasIndex(e => e.TelegramChatId)
                    .IsUnique()
                    .HasFilter("\"TelegramChatId\" IS NOT NULL");

                entity.HasIndex(e => e.Role);

                // Свойства
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TelegramUsername)
                    .HasMaxLength(50);

                entity.Property(e => e.TelegramChatId)
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Client");

                // Поля аутентификации
                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RefreshToken)
                    .HasMaxLength(500);

                entity.Property(e => e.RefreshTokenExpiryTime);

                // Базовые поля
                entity.Property(e => e.LastLoginAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt);

                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });

            // Trainer
            modelBuilder.Entity<Trainer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithOne()
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Bio).HasMaxLength(1000);
                entity.Property(e => e.PhotoUrl).HasMaxLength(500);
                entity.Property(e => e.Instagram).HasMaxLength(100);

                entity.Property(e => e.DanceStyles)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    )
                    .HasColumnName("DanceStyles")
                    .HasColumnType("text");
            });

            // Service
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasPrecision(10, 2);
            });

            // Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.Bookings)
                    .HasForeignKey(e => e.TrainerId);
                entity.HasOne(e => e.Service)
                    .WithMany()
                    .HasForeignKey(e => e.ServiceId);

                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Status);
            });

            // Subscription
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Subscriptions)
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Service)
                    .WithMany()
                    .HasForeignKey(e => e.ServiceId);
            });

            modelBuilder.Entity<ScheduleSlot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Trainer)
                    .WithMany(t => t.ScheduleSlots)
                    .HasForeignKey(e => e.TrainerId);

                entity.HasOne(e => e.Booking)
                    .WithOne()
                    .HasForeignKey<ScheduleSlot>(e => e.Id)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.TrainerId, e.StartTime }).IsUnique();
            });
        }
    }
}