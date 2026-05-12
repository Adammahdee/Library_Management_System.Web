using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Library_Management_System.Web.Services
{
    public class OverdueMonitorHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueMonitorHostedService> _logger;

        public OverdueMonitorHostedService(IServiceProvider serviceProvider, ILogger<OverdueMonitorHostedService> logger)
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
                    var borrowService = scope.ServiceProvider.GetRequiredService<IBorrowService>();
                    var processed = await borrowService.RunOverdueAutomationAsync();
                    _logger.LogInformation("Overdue monitor ran successfully. Processed overdue items: {Count}", processed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Overdue monitor failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}
