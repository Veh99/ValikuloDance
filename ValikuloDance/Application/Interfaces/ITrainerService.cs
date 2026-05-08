using ValikuloDance.Application.DTOs.Trainer;

namespace ValikuloDance.Application.Interfaces
{
    public interface ITrainerService
    {
        Task Add(TrainerDto trainerDto);
        Task<List<TrainerWorkingHourDto>> UpdateWorkingHoursAsync(Guid trainerId, UpdateTrainerWorkingHoursRequest request);
        Task<List<TrainerScheduleOverrideDto>> GetScheduleOverridesAsync(Guid trainerId, DateTime? from = null, DateTime? to = null);
        Task<TrainerScheduleOverrideDto> CreateScheduleOverrideAsync(Guid trainerId, UpsertTrainerScheduleOverrideRequest request);
        Task<TrainerScheduleOverrideDto> UpdateScheduleOverrideAsync(Guid trainerId, Guid overrideId, UpsertTrainerScheduleOverrideRequest request);
        Task DeleteScheduleOverrideAsync(Guid trainerId, Guid overrideId);
    }
}
