using HeriStep.API.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HeriStep.API.Services
{
    public class VisitProcessingWorker : BackgroundService
    {
        private readonly VisitQueueService _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VisitProcessingWorker> _logger;

        public VisitProcessingWorker(VisitQueueService queue, IServiceProvider serviceProvider, ILogger<VisitProcessingWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VisitProcessingWorker is starting.");

            await foreach (var visit in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<HeriStepDbContext>();
                    
                    // Ghi đè thời gian server để chính xác
                    visit.CreatedAtServer = DateTime.UtcNow;

                    context.StallVisits.Add(visit);
                    await context.SaveChangesAsync(stoppingToken);

                    // Comment ra trên production để đỡ trôi log, bật khi dev
                    // _logger.LogInformation($"Successfully processed visit for StallId: {visit.StallId}, DeviceId: {visit.DeviceId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing visit for StallId: {visit.StallId}");
                }
            }

            _logger.LogInformation("VisitProcessingWorker is stopping.");
        }
    }
}
