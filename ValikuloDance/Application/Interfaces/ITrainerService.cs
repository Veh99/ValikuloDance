using ValikuloDance.Application.DTOs.Trainer;

namespace ValikuloDance.Application.Interfaces
{
    public interface ITrainerService
    {
        Task Add(TrainerDto trainerDto);
    }
}
