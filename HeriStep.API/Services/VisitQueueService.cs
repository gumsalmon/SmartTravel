using System.Threading.Channels;
using HeriStep.Shared.Models;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;

namespace HeriStep.API.Services
{
    public class VisitQueueService
    {
        private readonly Channel<StallVisit> _queue;

        public VisitQueueService()
        {
            // Sử dụng BoundedChannel giới hạn 5000 phần tử để tránh sập RAM
            var options = new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<StallVisit>(options);
        }

        public async ValueTask EnqueueVisitAsync(StallVisit visit)
        {
            if (visit == null) throw new ArgumentNullException(nameof(visit));
            await _queue.Writer.WriteAsync(visit);
        }

        public IAsyncEnumerable<StallVisit> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
