namespace ValikuloDance.Application.Services
{
    public class BookingStatusBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingStatusBackgroundService> _logger;

        public BookingStatusBackgroundService(IServiceProvider serviceProvider, ILogger<BookingStatusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<BookingService>();
                    var completedCount = await bookingService.CompleteExpiredBookingsAsync();

                    if (completedCount > 0)
                    {
                        _logger.LogInformation("Marked {CompletedCount} bookings as completed.", completedCount);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update expired booking statuses.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
