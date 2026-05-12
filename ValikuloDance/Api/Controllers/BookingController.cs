using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Application.DTOs.Booking;
using ValikuloDance.Application.DTOs.Subscription;
using ValikuloDance.Application.Services;

namespace ValikuloDance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly SubscriptionService _subscriptionService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(BookingService bookingService, SubscriptionService subscriptionService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var result = await _bookingService.CreateBookingAsync(request, User);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании записи");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [Authorize]
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroupBooking([FromBody] CreateGroupBookingRequest request)
    {
        try
        {
            var result = await _subscriptionService.CreateGroupBookingAsync(request, User);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании групповой записи");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("available-dates/{trainerId}")]
    public async Task<IActionResult> GetAvailableDates(Guid trainerId, [FromQuery] Guid serviceId, [FromQuery] int days = 14)
    {
        var dates = await _bookingService.GetAvailableDatesAsync(trainerId, serviceId, days);
        return Ok(dates);
    }

    [HttpGet("available-slots/{trainerId}")]
    public async Task<IActionResult> GetAvailableSlots(Guid trainerId, [FromQuery] Guid serviceId, [FromQuery] DateTime date)
    {
        var slots = await _bookingService.GetAvailableSlotsAsync(trainerId, serviceId, date);
        return Ok(slots);
    }

    [HttpGet("group-slots")]
    public async Task<IActionResult> GetGroupSlots([FromQuery] Guid serviceId, [FromQuery] int days = 21)
    {
        var slots = await _subscriptionService.GetUpcomingGroupSlotsAsync(serviceId, days);
        return Ok(slots);
    }

    [Authorize]
    [HttpGet("user-bookings")]
    public async Task<IActionResult> GetUserBookings()
    {
        var bookings = await _bookingService.GetUserBookingsAsync(User);
        return Ok(bookings);
    }

    [Authorize]
    [HttpGet("trainer-bookings")]
    public async Task<IActionResult> GetTrainerBookings()
    {
        try
        {
            var bookings = await _bookingService.GetTrainerBookingsAsync(User);
            return Ok(bookings);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{bookingId}/confirm")]
    public async Task<IActionResult> ConfirmBooking(Guid bookingId)
    {
        try
        {
            var booking = await _bookingService.ConfirmBookingAsync(bookingId, User);
            return Ok(booking);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{bookingId}/trainer-cancel")]
    public async Task<IActionResult> CancelBookingByTrainer(Guid bookingId)
    {
        try
        {
            var booking = await _bookingService.CancelBookingByTrainerAsync(bookingId, User);
            return Ok(booking);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{bookingId}")]
    public async Task<IActionResult> CancelBooking(Guid bookingId)
    {
        try
        {
            var result = await _bookingService.CancelBookingAsync(bookingId, User);
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
