using System.Collections.Concurrent;
using HeriStep.Client.Models.LocalModels;
using HeriStep.Client.Services.Location;
using Microsoft.Maui.ApplicationModel;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// ─────────────────────────────────────────────────────────────────
    /// GEOFENCE ENGINE — Chế độ Khám Phá Tự Do (Free Discovery Mode)
    /// ─────────────────────────────────────────────────────────────────
    /// Luồng hoạt động:
    ///   1. Start() → nạp toàn bộ Stalls vào RAM (ConcurrentDictionary)
    ///   2. Vòng lặp 5s → lấy GPS → Haversine → kiểm tra IsVisited
    ///   3. Trigger: dist ≤ 20m AND IsVisited == false
    ///   4. Phát TTS bất đồng bộ (không block UI Thread)
    ///   5. Đánh dấu IsVisited = true trên RAM → Task.Run INSERT StallVisit
    /// ─────────────────────────────────────────────────────────────────
    /// </summary>
    public class GeofenceEngine : IAsyncDisposable
    {
        // ── Tunable constants ───────────────────────────────────────────
        private const double TriggerRadiusMeters = 20.0;
        private const int PollingIntervalMs      = 5_000;

        // ── Dependencies ────────────────────────────────────────────────
        private readonly ILocationService    _locationService;
        private readonly LocalDatabaseService _db;
        private readonly AudioTranslationService _audioService;

        // ── In-RAM stall state ──────────────────────────────────────────
        // Key = StallId, Value = stall snapshot với IsVisited được quản lý trên RAM
        private ConcurrentDictionary<int, StallRuntimeState> _stallMap = new();

        // ── Loop control ────────────────────────────────────────────────
        private CancellationTokenSource? _cts;
        private Task?                    _loopTask;

        // ── Session ─────────────────────────────────────────────────────
        private string _sessionId = string.Empty;

        // ── Events (để ViewModel/UI subscribe nếu cần) ─────────────────
        public event Action<LocalStall, double>? StallEntered;   // (stall, distMeters)
        public event Action<string>?             ErrorOccurred;  // log lỗi ra UI nếu cần

        // ── TTS semaphore: chỉ 1 TTS chạy tại một thời điểm ───────────
        private readonly SemaphoreSlim _ttsSemaphore = new(1, 1);

        public bool IsRunning => _loopTask is { IsCompleted: false };

        // ───────────────────────────────────────────────────────────────
        public GeofenceEngine(ILocationService locationService, LocalDatabaseService db, AudioTranslationService audioService)
        {
            _locationService = locationService;
            _db              = db;
            _audioService    = audioService;
        }

        // ═══════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Bật chế độ Khám Phá Tự Do.
        /// Gọi từ ViewModel, an toàn để gọi nhiều lần (idempotent).
        /// </summary>
        public async Task StartAsync()
        {
            if (IsRunning) return;

            // Tạo SessionId mới cho phiên này
            _sessionId = Guid.NewGuid().ToString("N")[..8];

            await LoadStallsIntoRamAsync();

            _cts      = new CancellationTokenSource();
            _loopTask = RunGpsLoopAsync(_cts.Token);

            Debug($"[GeofenceEngine] Started — session={_sessionId}, stalls={_stallMap.Count}");
        }

        /// <summary>
        /// Dừng chế độ Khám Phá Tự Do — huỷ token và đợi loop kết thúc sạch.
        /// </summary>
        public async Task StopAsync()
        {
            if (_cts is null) return;

            _cts.Cancel();

            try { if (_loopTask is not null) await _loopTask; }
            catch (OperationCanceledException) { /* expected */ }

            _cts.Dispose();
            _cts      = null;
            _loopTask = null;

            Debug("[GeofenceEngine] Stopped.");
        }

        /// <summary>
        /// Reset cờ IsVisited của tất cả sạp về false (dùng khi khách muốn nghe lại).
        /// Không cần dừng loop — thread-safe qua ConcurrentDictionary.
        /// </summary>
        public void ResetVisitedFlags()
        {
            foreach (var key in _stallMap.Keys)
                if (_stallMap.TryGetValue(key, out var state))
                    state.IsVisited = false;
        }

        /// <summary>
        /// Inject tọa độ giả lập từ Map bot test để kích hoạt đúng pipeline geofence hiện tại.
        /// </summary>
        public async Task InjectLocationAsync(double latitude, double longitude)
        {
            if (_stallMap.Count == 0)
            {
                await LoadStallsIntoRamAsync();
            }

            await CheckProximityAsync(latitude, longitude, CancellationToken.None);
        }

        // ═══════════════════════════════════════════════════════════════
        //  STEP 1 — NẠP STALLS VÀO RAM
        // ═══════════════════════════════════════════════════════════════

        private async Task LoadStallsIntoRamAsync()
        {
            var stalls = await _db.GetStallsAsync();

            _stallMap.Clear();

            foreach (var s in stalls)
            {
                // Bỏ qua sạp chưa có tọa độ
                if (s.Latitude == 0 && s.Longitude == 0) continue;

                _stallMap[s.Id] = new StallRuntimeState
                {
                    Stall     = s,
                    IsVisited = false     // ← flag trên RAM, KHÔNG đọc từ DB
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  STEP 2 — VÒNG LẶP GPS (chạy trên ThreadPool, KHÔNG phải UI)
        // ═══════════════════════════════════════════════════════════════

        private async Task RunGpsLoopAsync(CancellationToken ct)
        {
            // Đưa hẳn ra khỏi UI thread ngay lập tức
            await Task.Yield();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var location = await _locationService.GetLocationAsync();

                    if (location is not null)
                    {
                        await CheckProximityAsync(location.Latitude, location.Longitude, ct);
                    }
                    else
                    {
                        Debug("[GeofenceEngine] GPS = null (signal lost), skipping tick.");
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // dừng sạch khi bị huỷ
                }
                catch (Exception ex)
                {
                    // ⚠️  KHÔNG throw — bắt hết để vòng lặp không bao giờ crash App
                    Debug($"[GeofenceEngine] GPS loop error: {ex.Message}");
                    ErrorOccurred?.Invoke(ex.Message);
                }

                // Chờ 5 giây, có thể bị huỷ giữa chừng
                try { await Task.Delay(PollingIntervalMs, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  STEP 3 — HAVERSINE GEOFENCING
        // ═══════════════════════════════════════════════════════════════

        private async Task CheckProximityAsync(double userLat, double userLng, CancellationToken ct)
        {
            // Snapshot keys để tránh race condition khi iterating
            var stallIds = _stallMap.Keys.ToList();

            foreach (var id in stallIds)
            {
                if (ct.IsCancellationRequested) break;
                if (!_stallMap.TryGetValue(id, out var state)) continue;

                // Đã phát rồi → bỏ qua (chặn lặp tức thì)
                if (state.IsVisited) continue;

                double dist = HaversineDistanceMeters(
                    userLat, userLng,
                    state.Stall.Latitude, state.Stall.Longitude);

                if (dist <= TriggerRadiusMeters)
                {
                    // ── STEP 5a: Khóa cờ trên RAM NGAY (thread-safe) ──
                    state.IsVisited = true;

                    Debug($"[GeofenceEngine] ✅ TRIGGERED: {state.Stall.Name} @ {dist:F1}m");

                    // ── STEP 4: Phát TTS (không block) ─────────────────
                    _ = SpeakAsync(state.Stall, ct);

                    // ── STEP 5b: Ghi log ra SQLite trên background thread
                    _ = LogVisitAsync(state.Stall, userLat, userLng, dist);

                    // ── Notify subscribers (ViewModel cập nhật UI) ─────
                    StallEntered?.Invoke(state.Stall, dist);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  HAVERSINE FORMULA — tính khoảng cách (mét) giữa 2 tọa độ GPS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Công thức Haversine thuần C# — không phụ thuộc MAUI API.
        /// Sai số &lt;0.5% trong phạm vi đô thị — hoàn toàn đủ cho 20m geofence.
        /// </summary>
        public static double HaversineDistanceMeters(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            const double R = 6_371_000.0; // bán kính Trái Đất (mét)

            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;

            static double ToRad(double deg) => deg * Math.PI / 180.0;
        }

        // ═══════════════════════════════════════════════════════════════
        //  STEP 4 — PHÁT TTS (không block UI Thread)
        // ═══════════════════════════════════════════════════════════════

        private async Task SpeakAsync(LocalStall stall, CancellationToken ct)
        {
            // Nếu TTS đang nói → xếp hàng chờ (không chen ngang)
            await _ttsSemaphore.WaitAsync(ct);
            try
            {
                var script = await _audioService.GetStallScriptAsync(stall.Id, L.CurrentLanguage)
                            ?? stall.TtsScript;
                if (string.IsNullOrWhiteSpace(script)) return;

                Debug($"[GeofenceEngine] 🔊 TTS: \"{script[..Math.Min(40, script.Length)]}...\"");
                await _audioService.SpeakAsync(script, L.CurrentLanguage);
            }
            catch (OperationCanceledException)
            {
                // Engine bị Stop() giữa chừng → dừng sạch
            }
            catch (Exception ex)
            {
                Debug($"[GeofenceEngine] TTS error: {ex.Message}");
            }
            finally
            {
                _ttsSemaphore.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  STEP 5b — GHI LOG VISIT VÀO SQLITE (background Task.Run)
        // ═══════════════════════════════════════════════════════════════

        private Task LogVisitAsync(LocalStall stall, double deviceLat, double deviceLng, double distMeters)
        {
            // Task.Run → chạy trên ThreadPool, không block vòng lặp GPS
            return Task.Run(async () =>
            {
                try
                {
                    var visit = new StallVisit
                    {
                        StallId        = stall.Id,
                        StallName      = stall.Name ?? string.Empty,
                        DeviceLat      = deviceLat,
                        DeviceLng      = deviceLng,
                        DistanceMeters = distMeters,
                        VisitedAt      = DateTime.UtcNow,
                        SessionId      = _sessionId,
                    };

                    await _db.InsertStallVisitAsync(visit);
                    Debug($"[GeofenceEngine] 📝 Visit logged: StallId={stall.Id}, dist={distMeters:F1}m");
                }
                catch (Exception ex)
                {
                    // Lỗi ghi DB không được làm crash engine
                    Debug($"[GeofenceEngine] Log visit error: {ex.Message}");
                }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static void Debug(string msg) =>
            System.Diagnostics.Debug.WriteLine(msg);

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _ttsSemaphore.Dispose();
        }

        // ── Inner DTO: trạng thái runtime của mỗi sạp trên RAM ─────────
        private sealed class StallRuntimeState
        {
            public required LocalStall Stall     { get; init; }
            public volatile bool       IsVisited; // volatile: thread-safe read/write
        }
    }
}
