using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HeriStep.API.Services
{
    public class VisitProcessingWorker : BackgroundService
    {
        // ================================================================
        // CẤU HÌNH BATCH
        // BatchSize    : Gom tối đa 100 bản ghi rồi INSERT 1 lần
        // FlushInterval: Dù chưa đủ 100, cứ sau 500ms thì INSERT luôn
        //               (tránh data bị treo trong RAM khi traffic thấp)
        // ================================================================
        private const int BatchSize      = 100;
        private const int FlushIntervalMs = 500;

        private readonly VisitQueueService                 _queue;
        private readonly IServiceProvider                  _serviceProvider;
        private readonly ILogger<VisitProcessingWorker>    _logger;

        public VisitProcessingWorker(
            VisitQueueService queue,
            IServiceProvider serviceProvider,
            ILogger<VisitProcessingWorker> logger)
        {
            _queue           = queue;
            _serviceProvider = serviceProvider;
            _logger          = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "VisitProcessingWorker (Batch Mode) started. BatchSize={BatchSize}, FlushInterval={FlushInterval}ms",
                BatchSize, FlushIntervalMs);

            while (!stoppingToken.IsCancellationRequested)
            {
                var batch = new List<StallVisit>(BatchSize);

                try
                {
                    // --------------------------------------------------------
                    // BƯỚC 1: Chờ (blocking) cho đến khi có ÍT NHẤT 1 item.
                    // Worker ngủ yên, không tốn CPU khi queue rỗng.
                    // --------------------------------------------------------
                    var first = await _queue.DequeueAsync(stoppingToken);
                    first.CreatedAtServer = DateTime.UtcNow;
                    batch.Add(first);

                    // --------------------------------------------------------
                    // BƯỚC 2: Drain ngay (non-blocking) các item đang chờ.
                    // Không cần chờ — lấy hết những gì đang có trong queue lúc này.
                    // --------------------------------------------------------
                    while (batch.Count < BatchSize && _queue.TryDequeue(out var next))
                    {
                        next.CreatedAtServer = DateTime.UtcNow;
                        batch.Add(next);
                    }

                    // --------------------------------------------------------
                    // BƯỚC 3: Nếu batch chưa đầy, đợi thêm tối đa FlushInterval.
                    // Sau đó INSERT luôn dù batch chỉ có vài item (tránh delay lâu).
                    // --------------------------------------------------------
                    if (batch.Count < BatchSize)
                    {
                        using var flushCts = CancellationTokenSource
                            .CreateLinkedTokenSource(stoppingToken);
                        flushCts.CancelAfter(FlushIntervalMs);

                        try
                        {
                            while (batch.Count < BatchSize)
                            {
                                var item = await _queue.DequeueAsync(flushCts.Token);
                                item.CreatedAtServer = DateTime.UtcNow;
                                batch.Add(item);
                            }
                        }
                        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                        {
                            // FlushInterval hết giờ → INSERT batch nhỏ, không chờ thêm
                        }
                    }

                    // --------------------------------------------------------
                    // BƯỚC 4: Bulk INSERT toàn bộ batch vào DB — chỉ 1 round-trip!
                    // Thay vì 100 lần INSERT đơn lẻ, giờ chỉ cần 1 lần AddRangeAsync.
                    // --------------------------------------------------------
                    using var scope   = _serviceProvider.CreateScope();
                    var context       = scope.ServiceProvider.GetRequiredService<HeriStepDbContext>();

                    await context.StallVisits.AddRangeAsync(batch, stoppingToken);
                    await context.SaveChangesAsync(stoppingToken);

                    // Bật dòng dưới khi debug để theo dõi throughput thực tế:
                    // _logger.LogInformation("Batch INSERT {Count} visits. Queue còn: {Pending}", batch.Count, _queue.PendingCount);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // App đang shutdown — thoát vòng lặp sạch sẽ
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Lỗi khi xử lý batch {Count} bản ghi. Dữ liệu trong batch bị bỏ qua.",
                        batch.Count);
                    // Tiếp tục vòng lặp — không để 1 batch lỗi làm dừng toàn bộ Worker
                }
            }

            _logger.LogInformation("VisitProcessingWorker is stopping.");
        }
    }
}
