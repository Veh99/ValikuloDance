using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.EntityFrameworkCore;
using System.Resources;
using ValikuloDance.Application.DTOs.Trainer;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;
using ValikuloDance.Properties;


namespace ValikuloDance.Application.Services
{
    public class TrainerService : ITrainerService
    {
        private readonly AppDbContext _context;
        private readonly ResourceService _resourceService;

        public TrainerService(AppDbContext context, ResourceService resourceService)
        {
            _context = context;
            _resourceService = resourceService;
        }

        public async Task Add(TrainerDto request)
        {
            var userExists = await _context.Users
                .AnyAsync(x => x.Id == request.UserId);

            if (!userExists)
                throw new InvalidOperationException("Такого пользователя нет, чтобы стать тренером сначала зарегистрируйтесь");

            var trainerExists = await _context.Trainers
                .AnyAsync(x => x.UserId == request.UserId);

            if (trainerExists)
                throw new InvalidOperationException("Данный пользователь уже является тренером");

            //var resourceManager = Resource.pyj;
            //var obj = resourceManager.GetObject("pyj");
            //var photoUrl = _resourceService.GetImageAsBase64(obj);

            var entity = new Trainer()
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Bio = request.Bio,
                DanceStyles = request.DanceStyles,
                ExperienceYears = request.ExperienceYears,
                PhotoUrl = request.PhotoUrl ?? string.Empty,
            };

            var requestedWorkingHours = request.WorkingHours.Where(hours => hours.IsWorking).ToList();
            var workingHours = requestedWorkingHours.Count > 0
                ? requestedWorkingHours.Select(hours => CreateWorkingHour(entity.Id, hours)).ToList()
                : Enumerable.Range(1, 6).Select(day => new TrainerWorkingHour
                {
                    Id = Guid.NewGuid(),
                    TrainerId = entity.Id,
                    DayOfWeek = (DayOfWeek)day,
                    StartTimeLocal = new TimeSpan(9, 0, 0),
                    EndTimeLocal = new TimeSpan(21, 0, 0),
                    SlotDurationMinutes = 15,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }).ToList();

            await _context.Trainers.AddAsync(entity);
            await _context.TrainerWorkingHours.AddRangeAsync(workingHours);

            var user = await _context.Users.FirstAsync(x => x.Id == request.UserId);
            user.Role = "Trainer";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<TrainerWorkingHourDto>> UpdateWorkingHoursAsync(Guid trainerId, UpdateTrainerWorkingHoursRequest request)
        {
            var trainer = await _context.Trainers
                .Include(t => t.WorkingHours)
                .FirstOrDefaultAsync(t => t.Id == trainerId && t.IsActive)
                ?? throw new KeyNotFoundException("Тренер не найден");

            if (request.WorkingHours.Count == 0)
                throw new InvalidOperationException("Добавьте хотя бы один рабочий интервал");

            var newWorkingHours = request.WorkingHours
                .Where(hours => hours.IsWorking)
                .Select(hours => CreateWorkingHour(trainer.Id, hours))
                .ToList();

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                _context.TrainerWorkingHours.RemoveRange(trainer.WorkingHours);
                await _context.SaveChangesAsync();

                await _context.TrainerWorkingHours.AddRangeAsync(newWorkingHours);
                await PurgeFutureFreeScheduleSlotsAsync(trainer.Id);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            });

            return newWorkingHours.Select(MapWorkingHour).ToList();
        }

        public async Task<List<TrainerScheduleOverrideDto>> GetScheduleOverridesAsync(Guid trainerId, DateTime? from = null, DateTime? to = null)
        {
            if (!await _context.Trainers.AnyAsync(t => t.Id == trainerId && t.IsActive))
                throw new KeyNotFoundException("Тренер не найден");

            var query = _context.TrainerScheduleOverrides
                .Where(o => o.TrainerId == trainerId && o.IsActive);

            if (from != null)
                query = query.Where(o => o.Date >= from.Value.Date);

            if (to != null)
                query = query.Where(o => o.Date <= to.Value.Date);

            var overrides = await query
                .OrderBy(o => o.Date)
                .ThenBy(o => o.StartTimeLocal)
                .ToListAsync();

            return overrides.Select(MapScheduleOverride).ToList();
        }

        public async Task<TrainerScheduleOverrideDto> CreateScheduleOverrideAsync(Guid trainerId, UpsertTrainerScheduleOverrideRequest request)
        {
            if (!await _context.Trainers.AnyAsync(t => t.Id == trainerId && t.IsActive))
                throw new KeyNotFoundException("Тренер не найден");

            var scheduleOverride = CreateScheduleOverride(trainerId, request);
            await _context.TrainerScheduleOverrides.AddAsync(scheduleOverride);
            await PurgeFreeScheduleSlotsForLocalDateAsync(trainerId, scheduleOverride.Date);
            await _context.SaveChangesAsync();

            return MapScheduleOverride(scheduleOverride);
        }

        public async Task<TrainerScheduleOverrideDto> UpdateScheduleOverrideAsync(Guid trainerId, Guid overrideId, UpsertTrainerScheduleOverrideRequest request)
        {
            var scheduleOverride = await _context.TrainerScheduleOverrides
                .FirstOrDefaultAsync(o => o.Id == overrideId && o.TrainerId == trainerId && o.IsActive)
                ?? throw new KeyNotFoundException("Исключение расписания не найдено");

            var oldDate = scheduleOverride.Date;
            ApplyScheduleOverride(scheduleOverride, request);
            scheduleOverride.UpdatedAt = DateTime.UtcNow;

            await PurgeFreeScheduleSlotsForLocalDateAsync(trainerId, oldDate);
            if (oldDate.Date != scheduleOverride.Date.Date)
                await PurgeFreeScheduleSlotsForLocalDateAsync(trainerId, scheduleOverride.Date);

            await _context.SaveChangesAsync();
            return MapScheduleOverride(scheduleOverride);
        }

        public async Task DeleteScheduleOverrideAsync(Guid trainerId, Guid overrideId)
        {
            var scheduleOverride = await _context.TrainerScheduleOverrides
                .FirstOrDefaultAsync(o => o.Id == overrideId && o.TrainerId == trainerId && o.IsActive)
                ?? throw new KeyNotFoundException("Исключение расписания не найдено");

            scheduleOverride.IsActive = false;
            scheduleOverride.UpdatedAt = DateTime.UtcNow;

            await PurgeFreeScheduleSlotsForLocalDateAsync(trainerId, scheduleOverride.Date);
            await _context.SaveChangesAsync();
        }

        private static TrainerWorkingHour CreateWorkingHour(Guid trainerId, TrainerWorkingHourDto hours)
        {
            if (string.IsNullOrWhiteSpace(hours.StartTimeLocal) || string.IsNullOrWhiteSpace(hours.EndTimeLocal))
                throw new InvalidOperationException("Для рабочего дня укажите начало и конец интервала");

            var start = ParseTime(hours.StartTimeLocal, nameof(hours.StartTimeLocal));
            var end = ParseTime(hours.EndTimeLocal, nameof(hours.EndTimeLocal));
            ValidateTimeRange(start, end);
            ValidateSlotDuration(hours.SlotDurationMinutes);

            return new TrainerWorkingHour
            {
                Id = Guid.NewGuid(),
                TrainerId = trainerId,
                DayOfWeek = hours.DayOfWeek,
                StartTimeLocal = start,
                EndTimeLocal = end,
                SlotDurationMinutes = hours.SlotDurationMinutes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        private static TrainerScheduleOverride CreateScheduleOverride(Guid trainerId, UpsertTrainerScheduleOverrideRequest request)
        {
            var scheduleOverride = new TrainerScheduleOverride
            {
                Id = Guid.NewGuid(),
                TrainerId = trainerId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            ApplyScheduleOverride(scheduleOverride, request);
            return scheduleOverride;
        }

        private static void ApplyScheduleOverride(TrainerScheduleOverride scheduleOverride, UpsertTrainerScheduleOverrideRequest request)
        {
            var type = NormalizeOverrideType(request.Type);
            var isDayOff = type == "DayOff";

            TimeSpan? start = null;
            TimeSpan? end = null;
            if (!isDayOff)
            {
                if (string.IsNullOrWhiteSpace(request.StartTimeLocal) || string.IsNullOrWhiteSpace(request.EndTimeLocal))
                    throw new InvalidOperationException("Для Available и Unavailable укажите начало и конец интервала");

                start = ParseTime(request.StartTimeLocal, nameof(request.StartTimeLocal));
                end = ParseTime(request.EndTimeLocal, nameof(request.EndTimeLocal));
                ValidateTimeRange(start.Value, end.Value);
            }

            if (request.SlotDurationMinutes != null)
                ValidateSlotDuration(request.SlotDurationMinutes.Value);

            scheduleOverride.Date = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Unspecified);
            scheduleOverride.Type = type;
            scheduleOverride.StartTimeLocal = start;
            scheduleOverride.EndTimeLocal = end;
            scheduleOverride.SlotDurationMinutes = request.SlotDurationMinutes;
            scheduleOverride.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        }

        private async Task PurgeFutureFreeScheduleSlotsAsync(Guid trainerId)
        {
            var now = DateTime.UtcNow;
            var slots = await _context.ScheduleSlots
                .Where(s => s.TrainerId == trainerId && s.StartTime >= now && !s.IsBooked && s.BookingId == null)
                .ToListAsync();

            _context.ScheduleSlots.RemoveRange(slots);
        }

        private async Task PurgeFreeScheduleSlotsForLocalDateAsync(Guid trainerId, DateTime localDate)
        {
            var timeZone = GetMoscowTimeZone();
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified), timeZone);
            var nextDayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.Date.AddDays(1), DateTimeKind.Unspecified), timeZone);
            var now = DateTime.UtcNow;

            var slots = await _context.ScheduleSlots
                .Where(s =>
                    s.TrainerId == trainerId &&
                    s.StartTime >= dayStartUtc &&
                    s.StartTime < nextDayStartUtc &&
                    s.StartTime >= now &&
                    !s.IsBooked &&
                    s.BookingId == null)
                .ToListAsync();

            _context.ScheduleSlots.RemoveRange(slots);
        }

        private static TrainerWorkingHourDto MapWorkingHour(TrainerWorkingHour hours)
        {
            return new TrainerWorkingHourDto
            {
                DayOfWeek = hours.DayOfWeek,
                IsWorking = true,
                StartTimeLocal = FormatTime(hours.StartTimeLocal),
                EndTimeLocal = FormatTime(hours.EndTimeLocal),
                SlotDurationMinutes = hours.SlotDurationMinutes
            };
        }

        private static TrainerScheduleOverrideDto MapScheduleOverride(TrainerScheduleOverride scheduleOverride)
        {
            return new TrainerScheduleOverrideDto
            {
                Id = scheduleOverride.Id,
                TrainerId = scheduleOverride.TrainerId,
                Date = scheduleOverride.Date,
                StartTimeLocal = scheduleOverride.StartTimeLocal == null ? null : FormatTime(scheduleOverride.StartTimeLocal.Value),
                EndTimeLocal = scheduleOverride.EndTimeLocal == null ? null : FormatTime(scheduleOverride.EndTimeLocal.Value),
                Type = scheduleOverride.Type,
                SlotDurationMinutes = scheduleOverride.SlotDurationMinutes,
                Reason = scheduleOverride.Reason,
                IsActive = scheduleOverride.IsActive
            };
        }

        private static string NormalizeOverrideType(string? type)
        {
            var normalized = type?.Trim();
            if (string.Equals(normalized, "Available", StringComparison.OrdinalIgnoreCase))
                return "Available";
            if (string.Equals(normalized, "Unavailable", StringComparison.OrdinalIgnoreCase))
                return "Unavailable";
            if (string.Equals(normalized, "DayOff", StringComparison.OrdinalIgnoreCase))
                return "DayOff";

            throw new InvalidOperationException("Тип исключения должен быть Available, Unavailable или DayOff");
        }

        private static TimeSpan ParseTime(string value, string fieldName)
        {
            if (TimeSpan.TryParse(value, out var result))
                return result;

            throw new InvalidOperationException($"Некорректное время в поле {fieldName}");
        }

        private static void ValidateTimeRange(TimeSpan start, TimeSpan end)
        {
            if (start >= end)
                throw new InvalidOperationException("Начало интервала должно быть раньше конца");
        }

        private static void ValidateSlotDuration(int duration)
        {
            if (duration < 5 || duration > 240)
                throw new InvalidOperationException("Длительность слота должна быть от 5 до 240 минут");
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
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
