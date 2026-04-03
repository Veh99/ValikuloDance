using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ValikuloDance.Application.DTOs.Services;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var services = await _context.Services
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Price,
                    s.DurationMinutes,
                    s.IsPackage,
                    s.SessionsCount,
                    s.Icon
                })
                .ToListAsync();

            return Ok(services);
        }

        [HttpPost("add-service")]
        [Authorize]
        public async Task<IActionResult> Add(AddServiceDto dto)
        {
            var service = new Service()
            {
                Id = dto.Id,
                CreatedAt = dto.CreatedAt,
                DurationMinutes = dto.DurationMinutes,
                Name = dto.Name,
                Price = dto.Price,
                IsActive = dto.IsActive,
                UpdatedAt = dto.UpdatedAt,
                Description = dto.Description,
                IsDeleted = dto.IsDeleted,
                IsPackage = dto.IsPackage,
            };

            await _context.Services.AddAsync(service);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("delete-service")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service is null)
                return NotFound("Услуга не найдена");
            
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
