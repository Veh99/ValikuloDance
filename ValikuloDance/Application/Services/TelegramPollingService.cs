using Telegram.Bot;
using Telegram.Bot.Types;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Application.Services
{
    public class TelegramPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramPollingService> _logger;
        private readonly ITelegramBotClient? _botClient;

        public TelegramPollingService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<TelegramPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var botToken = configuration["Telegram:BotToken"];
            if (!string.IsNullOrWhiteSpace(botToken))
            {
                _botClient = new TelegramBotClient(botToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_botClient == null)
            {
                _logger.LogWarning("Telegram polling disabled: bot token is not configured.");
                return;
            }

            var offset = 0;

            try
            {
                await _botClient.DeleteWebhook(cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Telegram webhook before polling.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var updates = await _botClient.GetUpdates(
                        offset: offset,
                        timeout: 30,
                        cancellationToken: stoppingToken);

                    foreach (var update in updates)
                    {
                        offset = update.Id + 1;

                        using var scope = _serviceProvider.CreateScope();
                        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                        await telegramService.HandleUpdateAsync(update);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Telegram polling iteration failed.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}
