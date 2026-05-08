using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
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
            .Include(t => t.ScheduleOverrides.Where(o => o.IsActive))
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
                t.Telegram,
                t.User.TelegramUsername,
                WorkingHours = t.WorkingHours.Select(w => new
                {
                    w.DayOfWeek,
                    w.StartTimeLocal,
                    w.EndTimeLocal,
                    w.SlotDurationMinutes
                }),
                ScheduleOverrides = t.ScheduleOverrides.Select(o => new
                {
                    o.Id,
                    o.Date,
                    o.StartTimeLocal,
                    o.EndTimeLocal,
                    o.Type,
                    o.SlotDurationMinutes,
                    o.Reason
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
            .Include(t => t.ScheduleOverrides.Where(o => o.IsActive))
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
            trainer.Telegram,
            trainer.User.TelegramUsername,
            WorkingHours = trainer.WorkingHours.Select(w => new
            {
                w.DayOfWeek,
                w.StartTimeLocal,
                w.EndTimeLocal,
                w.SlotDurationMinutes
            }),
            ScheduleOverrides = trainer.ScheduleOverrides.Select(o => new
            {
                o.Id,
                o.Date,
                o.StartTimeLocal,
                o.EndTimeLocal,
                o.Type,
                o.SlotDurationMinutes,
                o.Reason
            })
        });
    }

    [HttpPost()]
    [Authorize]
    public async Task<IActionResult> Add(TrainerDto request)
    {
        await _trainerService.Add(request);
        return Ok();
    }

    [HttpPut("{id}/working-hours")]
    [Authorize]
    public async Task<IActionResult> UpdateWorkingHours(Guid id, [FromBody] UpdateTrainerWorkingHoursRequest request)
    {
        var workingHours = await _trainerService.UpdateWorkingHoursAsync(id, request);
        return Ok(workingHours);
    }

    [HttpGet("{id}/schedule-overrides")]
    public async Task<IActionResult> GetScheduleOverrides(Guid id, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var overrides = await _trainerService.GetScheduleOverridesAsync(id, from, to);
        return Ok(overrides);
    }

    [HttpPost("{id}/schedule-overrides")]
    [Authorize]
    public async Task<IActionResult> CreateScheduleOverride(Guid id, [FromBody] UpsertTrainerScheduleOverrideRequest request)
    {
        var scheduleOverride = await _trainerService.CreateScheduleOverrideAsync(id, request);
        return Ok(scheduleOverride);
    }

    [HttpPut("{id}/schedule-overrides/{overrideId}")]
    [Authorize]
    public async Task<IActionResult> UpdateScheduleOverride(Guid id, Guid overrideId, [FromBody] UpsertTrainerScheduleOverrideRequest request)
    {
        var scheduleOverride = await _trainerService.UpdateScheduleOverrideAsync(id, overrideId, request);
        return Ok(scheduleOverride);
    }

    [HttpDelete("{id}/schedule-overrides/{overrideId}")]
    [Authorize]
    public async Task<IActionResult> DeleteScheduleOverride(Guid id, Guid overrideId)
    {
        await _trainerService.DeleteScheduleOverrideAsync(id, overrideId);
        return NoContent();
    }
}
