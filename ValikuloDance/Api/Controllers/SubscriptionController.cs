using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValikuloDance.Application.DTOs.Subscription;
using ValikuloDance.Application.Services;

namespace ValikuloDance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans([FromQuery] string? format = null)
        {
            var plans = await _subscriptionService.GetPlansAsync(format);
            return Ok(plans);
        }

        [Authorize]
        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateSubscriptionRequestDto request)
        {
            try
            {
                var subscription = await _subscriptionService.CreateRequestAsync(request, User);
                return Ok(subscription);
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
        [HttpGet("my")]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(User);
            return Ok(subscriptions);
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] string format)
        {
            var subscriptions = await _subscriptionService.GetActiveSubscriptionsAsync(User, format);
            return Ok(subscriptions);
        }

        [Authorize]
        [HttpGet("my-group-schedules")]
        public async Task<IActionResult> GetMyGroupSchedules()
        {
            try
            {
                var schedules = await _subscriptionService.GetMyGroupLessonSchedulesAsync(User);
                return Ok(schedules);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("my-group-schedules")]
        public async Task<IActionResult> CreateMyGroupSchedule([FromBody] UpsertGroupLessonScheduleRequest request)
        {
            try
            {
                var schedule = await _subscriptionService.CreateMyGroupLessonScheduleAsync(request, User);
                return Ok(schedule);
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
        [HttpPut("my-group-schedules/{scheduleId}")]
        public async Task<IActionResult> UpdateMyGroupSchedule(Guid scheduleId, [FromBody] UpsertGroupLessonScheduleRequest request)
        {
            try
            {
                var schedule = await _subscriptionService.UpdateMyGroupLessonScheduleAsync(scheduleId, request, User);
                return Ok(schedule);
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
        [HttpDelete("my-group-schedules/{scheduleId}")]
        public async Task<IActionResult> DeleteMyGroupSchedule(Guid scheduleId)
        {
            try
            {
                await _subscriptionService.DeleteMyGroupLessonScheduleAsync(scheduleId, User);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
