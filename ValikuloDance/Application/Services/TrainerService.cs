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

            await _context.Trainers.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
    }
}
