using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Application.DTOs;
using ValikuloDance.Application.Services;

namespace ValikuloDance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(BookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(request);
            return Ok(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании записи");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("available-slots/{trainerId}")]
    public async Task<IActionResult> GetAvailableSlots(Guid trainerId, [FromQuery] DateTime date)
    {
        var slots = await _bookingService.GetAvailableSlotsAsync(trainerId, date);
        return Ok(slots);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBookings(Guid userId)
    {
        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpDelete("{bookingId}")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, [FromQuery] Guid userId)
    {
        try
        {
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);
            if (!result)
                return NotFound(new { error = "Запись не найдена" });

            return Ok(new { message = "Запись успешно отменена" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
