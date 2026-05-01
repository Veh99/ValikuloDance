namespace ValikuloDance.Application.Services
{
    public class SubscriptionMaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionMaintenanceBackgroundService> _logger;

        public SubscriptionMaintenanceBackgroundService(IServiceProvider serviceProvider, ILogger<SubscriptionMaintenanceBackgroundService> logger)
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
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();

                    var expiredPending = await subscriptionService.ExpirePendingSubscriptionsAsync();
                    var expiredActive = await subscriptionService.ExpireActiveSubscriptionsAsync();

                    if (expiredPending > 0 || expiredActive > 0)
                    {
                        _logger.LogInformation(
                            "Subscription maintenance updated statuses. Pending expired: {ExpiredPending}; active expired: {ExpiredActive}.",
                            expiredPending,
                            expiredActive);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to maintain subscriptions.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
