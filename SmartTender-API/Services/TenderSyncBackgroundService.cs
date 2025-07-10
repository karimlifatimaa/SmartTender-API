using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTender.Application.Interfaces;

namespace SmartTender_API.Services
{
    public class TenderSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TenderSyncBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(3);

        public TenderSyncBackgroundService(IServiceProvider serviceProvider, ILogger<TenderSyncBackgroundService> logger)
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
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tenderService = scope.ServiceProvider.GetRequiredService<ITenderService>();
                        await tenderService.SyncTendersFromRemoteAsync();
                        _logger.LogInformation("Tender sync completed at: {time}", DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during tender sync");
                }
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
} 