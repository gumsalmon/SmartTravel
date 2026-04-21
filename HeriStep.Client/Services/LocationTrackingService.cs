using System.Net.Http.Json;

namespace HeriStep.Client.Services;

public class LocationTrackingService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cts;
    private Task? _trackingTask;
    private readonly SemaphoreSlim _trackLock = new(1, 1);
    private const int TrackingIntervalMs = 15_000;

    public bool IsRunning => _trackingTask is { IsCompleted: false };

    public LocationTrackingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task StartAsync()
    {
        if (IsRunning) return Task.CompletedTask;

        _cts = new CancellationTokenSource();
        _trackingTask = RunTrackingLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts == null) return;

        _cts.Cancel();
        try
        {
            if (_trackingTask != null)
            {
                await _trackingTask;
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACKING] StopAsync failed: {ex.Message}");
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _trackingTask = null;
        }
    }

    private async Task RunTrackingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _trackLock.WaitAsync(token);
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.GetLocationAsync(request, token);
                    if (location != null)
                    {
                        var payload = new TrackPayload
                        {
                            DeviceId = GetDeviceId(),
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            RecordedAt = DateTime.UtcNow
                        };

                        await _httpClient.PostAsJsonAsync("api/analytics/track", payload, token);
                    }
                }
                finally
                {
                    _trackLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRACKING] Loop error: {ex.Message}");
            }

            try
            {
                await Task.Delay(TrackingIntervalMs, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static string GetDeviceId()
    {
        try
        {
            var cached = Preferences.Default.Get("tracking_device_id", string.Empty);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            var generated = $"{DeviceInfo.Current.Platform}_{DeviceInfo.Current.Model}_{Guid.NewGuid():N}";
            Preferences.Default.Set("tracking_device_id", generated);
            return generated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACKING] GetDeviceId failed: {ex.Message}");
            return $"UNKNOWN_{Guid.NewGuid():N}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _trackLock.Dispose();
    }

    private class TrackPayload
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
