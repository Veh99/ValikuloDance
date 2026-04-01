using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ValikuloDance.Application.DTOs.Booking;

namespace ValikuloDance.Application.Services
{
    public class BookingService
    {
        private readonly AppDbContext _context;
        private readonly ITelegramService _telegramService;

        public BookingService(AppDbContext context, ITelegramService telegramService)
        {
            _context = context;
            _telegramService = telegramService;
        }

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
        {
            // Проверяем существование пользователя
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                throw new Exception("Пользователь не найден");

            // Проверяем существование тренера
            var trainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == request.TrainerId);
            if (trainer == null)
                throw new Exception("Тренер не найден");

            // Проверяем существование услуги
            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
                throw new Exception("Услуга не найдена");

            // Проверяем, свободно ли время
            var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
            var endTime = startTimeUtc.AddMinutes((double)service.DurationMinutes);
            var existingBooking = await _context.Bookings
                .AnyAsync(b => b.TrainerId == request.TrainerId &&
                               b.StartTime < endTime &&
                               b.EndTime > request.StartTime &&
                               b.Status != "Cancelled");

            if (existingBooking)
                throw new Exception("Это время уже занято");

            // Создаем запись
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                TrainerId = request.TrainerId,
                ServiceId = request.ServiceId,
                StartTime = request.StartTime,
                CreatedAt = DateTime.UtcNow,
                EndTime = endTime,
                Notes = request.Notes,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Отправляем уведомления в Telegram
            await _telegramService.SendBookingConfirmationAsync(booking);

            return new BookingResponse
            {
                Id = booking.Id,
                UserName = user.Name,
                TrainerName = trainer.User.Name,
                ServiceName = service.Name,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                Price = service.Price
            };
        }

        public async Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(Guid trainerId, DateTime date)
        {
            var slots = new List<AvailableSlotResponse>();

            // Приводим дату к UTC
            var utcDate = date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
                : date.Date.ToUniversalTime();

            // Рабочие часы (9:00 - 21:00) - предполагаем, что это локальное время
            // Конвертируем локальное время в UTC (например, для Москвы UTC+3)
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            var startHour = 9;
            var endHour = 21;

            for (var hour = startHour; hour < endHour; hour++)
            {
                // Создаем локальное время
                var localStartTime = new DateTime(utcDate.Year, utcDate.Month, utcDate.Day, hour, 0, 0);
                var localEndTime = localStartTime.AddHours(1);

                // Конвертируем в UTC для сравнения с БД
                var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localStartTime, timeZone);
                var endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localEndTime, timeZone);

                var isBooked = await _context.Bookings
                    .AnyAsync(b => b.TrainerId == trainerId &&
                                   b.StartTime < endTimeUtc &&
                                   b.EndTime > startTimeUtc &&
                                   b.Status != "Cancelled");

                slots.Add(new AvailableSlotResponse
                {
                    StartTime = startTimeUtc,
                    EndTime = endTimeUtc,
                    IsAvailable = !isBooked
                });
            }

            return slots;
        }

        public async Task<List<BookingResponse>> GetUserBookingsAsync(Guid userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .Where(b => b.UserId == userId && b.StartTime >= DateTime.UtcNow)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return bookings.Select(b => new BookingResponse
            {
                Id = b.Id,
                UserName = b.User.Name,
                TrainerName = b.Trainer.User.Name,
                ServiceName = b.Service.Name,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                Price = b.Service.Price
            }).ToList();
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return false;

            if (booking.StartTime < DateTime.UtcNow.AddHours(2))
                throw new Exception("Отмена возможна не позднее чем за 2 часа до занятия");

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Отправляем уведомление об отмене
            await _telegramService.SendBookingCancellationAsync(booking);

            return true;
        }
    }
}
