using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ValikuloDance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainerController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrainerController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
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
                t.User.TelegramUsername
            })
            .ToListAsync();

        return Ok(trainers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.User)
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
            trainer.User.TelegramUsername
        });
    }
}
