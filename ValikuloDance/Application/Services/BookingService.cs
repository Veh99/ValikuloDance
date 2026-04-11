using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ValikuloDance.Application.DTOs.Booking;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;

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

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst("sub")?.Value
               ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? userClaims.FindFirst("id")?.Value
               ?? userClaims.FindFirst("userId")?.Value
               ?? userClaims.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Некорректный идентификатор пользователя");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Пользователь не найден");

            var trainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == request.TrainerId);

            if (trainer == null)
                throw new KeyNotFoundException("Тренер не найден");

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
                throw new KeyNotFoundException("Услуга не найдена");

            var startTimeUtc = request.StartTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc)
                : request.StartTime.ToUniversalTime();

            if (startTimeUtc < DateTime.UtcNow)
                throw new InvalidOperationException("Нельзя создать запись на прошедшее время");

            var endTimeUtc = startTimeUtc.AddMinutes((double)service.DurationMinutes);

            var exists = await _context.Bookings.AnyAsync(b =>
                b.TrainerId == request.TrainerId &&
                b.StartTime < endTimeUtc &&
                b.EndTime > startTimeUtc &&
                b.Status != "Cancelled"
            );

            if (exists)
                throw new InvalidOperationException("Это время уже занято");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TrainerId = request.TrainerId,
                ServiceId = request.ServiceId,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                CreatedAt = DateTime.UtcNow,
                Notes = request.Notes,
                Status = "Confirmed"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var bookingForNotification = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstAsync(b => b.Id == booking.Id);

            await _telegramService.SendBookingConfirmationAsync(bookingForNotification);

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
            var trainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null)
                throw new KeyNotFoundException("Тренер не найден");

            var slots = new List<AvailableSlotResponse>();

            var timeZone = GetMoscowTimeZone();
            var localDate = date.Kind switch
            {
                DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(date, timeZone).Date,
                DateTimeKind.Local => date.ToLocalTime().Date,
                _ => date.Date
            };
            var startHour = 9;
            var endHour = 21;

            for (var hour = startHour; hour < endHour; hour++)
            {
                var localStartTime = new DateTime(localDate.Year, localDate.Month, localDate.Day, hour, 0, 0, DateTimeKind.Unspecified);
                var localEndTime = localStartTime.AddHours(1);

                var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localStartTime, timeZone);
                var endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localEndTime, timeZone);

                var isBooked = await _context.Bookings.AnyAsync(b =>
                    b.TrainerId == trainerId &&
                    b.StartTime < endTimeUtc &&
                    b.EndTime > startTimeUtc &&
                    b.Status != "Cancelled"
                );

                slots.Add(new AvailableSlotResponse
                {
                    StartTime = startTimeUtc,
                    EndTime = endTimeUtc,
                    IsAvailable = !isBooked
                });
            }

            return slots;
        }

        public async Task<List<BookingResponse>> GetUserBookingsAsync(ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst("sub")?.Value
               ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? userClaims.FindFirst("id")?.Value
               ?? userClaims.FindFirst("userId")?.Value
               ?? userClaims.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new ArgumentException("Неверный формат ID пользователя");

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .Where(b => b.UserId == userId)
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

        public async Task<bool> CancelBookingAsync(Guid bookingId, ClaimsPrincipal userClaims)
        {
            var userIdClaim = userClaims.FindFirst("sub")?.Value
               ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? userClaims.FindFirst("id")?.Value
               ?? userClaims.FindFirst("userId")?.Value
               ?? userClaims.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Пользователь не авторизован");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new ArgumentException("Неверный формат ID пользователя");

            var booking = await GetBookingAsync(bookingId, userId);

            if (booking == null)
                return false;

            if (booking.StartTime < DateTime.UtcNow.AddHours(1))
                throw new InvalidOperationException("Отмена возможна не позднее чем за 2 часа до занятия");

            booking.Status = "Cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Remove(booking);
            await _context.SaveChangesAsync();

            await _telegramService.SendBookingCancellationAsync(booking);

            return true;
        }

        private async Task<Booking?> GetBookingAsync(Guid bookingId, Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Trainer).ThenInclude(t => t.User)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
        }

        private static TimeZoneInfo GetMoscowTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            }
        }
    }
}
