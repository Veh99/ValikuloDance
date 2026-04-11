using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ValikuloDance.Application.DTOs.Trainer;
using System.Reflection.Metadata;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITrainerService _trainerService;

    public TrainerController(AppDbContext context, ITrainerService trainerService)
    {
        _context = context;
        _trainerService = trainerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.WorkingHours.Where(w => w.IsActive))
            .Where(t => t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.User.Name,
                t.Bio,
                t.PhotoUrl,
                t.ExperienceYears,
                t.DanceStyles,
                t.Instagram,
                t.User.TelegramUsername,
                WorkingHours = t.WorkingHours.Select(w => new
                {
                    w.DayOfWeek,
                    w.StartTimeLocal,
                    w.EndTimeLocal,
                    w.SlotDurationMinutes
                })
            })
            .ToListAsync();

        return Ok(trainers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.WorkingHours.Where(w => w.IsActive))
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (trainer == null)
            return NotFound();

        return Ok(new
        {
            trainer.Id,
            trainer.User.Name,
            trainer.Bio,
            trainer.PhotoUrl,
            trainer.ExperienceYears,
            trainer.DanceStyles,
            trainer.Instagram,
            trainer.User.TelegramUsername,
            WorkingHours = trainer.WorkingHours.Select(w => new
            {
                w.DayOfWeek,
                w.StartTimeLocal,
                w.EndTimeLocal,
                w.SlotDurationMinutes
            })
        });
    }

    [HttpPost()]
    public async Task<IActionResult> Add(TrainerDto request)
    {
        await _trainerService.Add(request);
        return Ok();
    }
}
