using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
    }
}
