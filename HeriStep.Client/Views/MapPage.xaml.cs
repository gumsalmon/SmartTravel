using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;
using HeriStep.Client.Services.Location;
using System;
using System.Threading;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using System.IO;
using System.Text.Json;
using LocalVisitModel = HeriStep.Client.Models.LocalModels.StallVisit;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private bool isPopupOpen = false;
    private Stall _currentSelectedShop = new Stall();
    private Stall? _nearestStall = null;
    private double _lastUserLat = 10.7595;
    private double _lastUserLon = 106.7025;
    private CancellationTokenSource? _locationLoopCts;
    private List<Stall> _allStalls = new();
    private bool _isTtsPlaying = false;
    private readonly AudioTranslationService _audioService;
    private readonly AudioManagerService _audioManager;
    private readonly LocalDatabaseService _localDb;
    private readonly GeofenceEngine _botGeofenceEngine; // Giữ lại cho tương thích dependency (nếu có sử dụng ở chỗ khác)
    private readonly LocationTrackingService _trackingService;

    // POI Clustering & Priority
    private HashSet<int> _visitedStallIds = new();
    private double _lastUserHeading = 0;

    private volatile bool _isPageActive;
    private bool _isMapReady = false;
    private bool _isUserInteracting = false;
    private DateTime _lastInteractionTime = DateTime.MinValue;
    private int _jsCallsInFlight = 0;
    private static readonly HttpClient _sharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public MapPage(AudioTranslationService audioService, AudioManagerService audioManager, LocalDatabaseService localDb, GeofenceEngine geofenceEngine, LocationTrackingService trackingService)
    {
        InitializeComponent();
        _audioService = audioService;
        _audioManager = audioManager;
        _trackingService = trackingService;

        LoadLeafletMap();
    }

    private async void LoadLeafletMap()
    {
        try
        {
            mapWebView.Navigated += (s, e) => {
                _isMapReady = true;
                if (_allStalls.Count > 0) UpdateWebViewStalls();
            };

            using var stream = await FileSystem.OpenAppPackageFileAsync("leaflet/map.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            string baseUrl = "";
#if ANDROID
            baseUrl = "file:///android_asset/leaflet/";
#endif
            mapWebView.Source = new HtmlWebViewSource { Html = html, BaseUrl = baseUrl };
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MAP_LOAD] " + ex.Message);
        }
    }

    private void mapWebView_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("maui://interaction/"))
        {
            e.Cancel = true;
            _isUserInteracting = e.Url.Contains("start");
            
            if (!_isUserInteracting)
            {
                _lastInteractionTime = DateTime.Now; // Start cooldown
            }
            return;
        }

        if (e.Url.StartsWith("maui://stallclick/"))
        {
            e.Cancel = true; 
            var idStr = e.Url.Replace("maui://stallclick/", "");
            if (int.TryParse(idStr, out int stallId))
            {
                var stall = _allStalls.FirstOrDefault(s => s.Id == stallId);
                if (stall != null) ShowStallPopup(stall);
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageActive = true;
        ClosePopup();
        ApplyLocalization();

        try
        {
            var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission == PermissionStatus.Granted)
            {
                await _trackingService.StartAsync();
            }
            else
            {
                Console.WriteLine("[TRACKING] Location permission denied, tracking skipped.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACKING] Start tracking failed: {ex.Message}");
        }

        await LoadStallsAsync();
        StartUserLocationLoop();

        _ = _audioService.WarmUpAsync();

        if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
        {
            Compass.Default.ReadingChanged += OnCompassReadingChanged;
            Compass.Default.Start(SensorSpeed.UI);
        }
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        _lastUserHeading = e.Reading.HeadingMagneticNorth;
    }

    private void ApplyLocalization()
    {
        lblNearbyLabel.Text = L.Get("map_nearby_label");
        btnGeofencePlayAudio.Text = L.Get("map_play_audio");
        btnPlayAudio.Text = L.Get("map_play_audio");
        btnNavigate.Text = L.Get("map_navigate");
        btnViewDetail.Text = L.Get("map_detail");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isPageActive = false;
        _locationLoopCts?.Cancel();
        
        if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
        {
            Compass.Default.ReadingChanged -= OnCompassReadingChanged;
            Compass.Default.Stop();
        }

        _ = Task.Run(async () =>
        {
            try { await _trackingService.StopAsync(); }
            catch (Exception ex) { Console.WriteLine($"[TRACKING] Stop tracking failed: {ex.Message}"); }
        });
    }

    private void ShowStallPopup(Stall stall)
    {
        if (stall == null) return;

        _currentSelectedShop = stall;
        popupName.Text = stall.Name;
        lblOwner.Text = $"👤 {(string.IsNullOrEmpty(stall.OwnerName) ? "Chưa có chủ" : stall.OwnerName)}";

        if (!stall.IsOpen)
        {
            lblStatusTag.Text = "⛔ Đóng";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B5B4E");
        }
        else if (stall.OwnerId == null)
        {
            lblStatusTag.Text = "🟢 Trống";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
        }
        else
        {
            lblStatusTag.Text = "🔴 Mở";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#D35400");
        }

        if (!string.IsNullOrEmpty(stall.ImageUrl))
        {
            popupImage.Source = stall.ImageUrl.StartsWith("http")
                ? stall.ImageUrl
                : $"{AppConstants.BaseApiUrl}{stall.ImageUrl}";
        }
        else
        {
            string[] foods = { "pho_bo.jpg","banh_mi.jpg","oc_len.jpg","bun_bo_hue.jpg",
                               "goi_cuon.jpg","hu_tieu.jpg","banh_xeo.jpg","che_ba_mau.jpg",
                               "ca_phe_trung.jpg","com_tam.jpg" };
            popupImage.Source = foods[Math.Abs(stall.Id) % foods.Length];
        }

        overlay.IsVisible = true;
        shopPopup.IsVisible = true;
    }

    // ══════════════════════════════════════════════
    // LOCATION LOOP
    // ══════════════════════════════════════════════

    private void StartUserLocationLoop()
    {
        _locationLoopCts?.Cancel();
        _locationLoopCts = new CancellationTokenSource();
        var token = _locationLoopCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                bool isRealGPS = false;
                try
                {
                    // LẤY VỊ TRÍ CUỐI CÙNG thay vì gửi yêu cầu GetLocationAsync mới.
                    // Yêu cầu phần cứng mới sẽ gây Deadlock với LocationTrackingService đang chạy ngầm.
                    var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                    if (loc != null) 
                    { 
                        _lastUserLat = loc.Latitude; 
                        _lastUserLon = loc.Longitude; 
                        isRealGPS = true; 
                    }
                }
                catch { }

                if (_isMapReady && _isPageActive)
                {
                    RunJsAsync($"if(typeof updateUserLocation === 'function') updateUserLocation({_lastUserLat}, {_lastUserLon}, {(isRealGPS ? "true" : "false")});");
                }
                
                CheckNearbyStalls(_lastUserLat, _lastUserLon);
                
                System.Diagnostics.Debug.WriteLine($"[MAP_LOOP] Stalls count: {_allStalls?.Count ?? 0}. Waiting 10s...");

                try { await Task.Delay(10000, token); }
                catch (TaskCanceledException) { break; }
            }
        }, token);
    }

    // ══════════════════════════════════════════════
    // GEOFENCING
    // ══════════════════════════════════════════════

    private void CheckNearbyStalls(double userLat, double userLon)
    {
        var globalRadius = Preferences.Default.Get("voice_radius", 50.0);
        var userLoc = new Microsoft.Maui.Devices.Sensors.Location(userLat, userLon);

        var candidates = new List<(Stall stall, double distance)>();

        foreach (var stall in _allStalls)
        {
            if (stall.Latitude == 0 && stall.Longitude == 0) continue;

            var stallLoc = new Microsoft.Maui.Devices.Sensors.Location(stall.Latitude, stall.Longitude);
            double dist = Microsoft.Maui.Devices.Sensors.Location
                .CalculateDistance(userLoc, stallLoc, DistanceUnits.Kilometers) * 1000;

            double r = stall.RadiusMeter > 0 ? Math.Min(stall.RadiusMeter, globalRadius) : globalRadius;

            if (dist <= r) 
            {
                candidates.Add((stall, dist));
            }
        }

        if (!candidates.Any()) return;

        // Luật 1: Gần nhất
        var ordered = candidates.OrderBy(c => c.distance).ToList();
        var nearestDist = ordered.First().distance;
        
        // Nhóm các POI có khoảng cách gần bằng nhau (sai số 2 mét)
        var topCollisions = ordered.Where(c => Math.Abs(c.distance - nearestDist) <= 2.0).ToList();

        Stall chosen = topCollisions.First().stall;

        if (topCollisions.Count > 1)
        {
            // Luật 2: Chưa chơi bao giờ
            var unseen = topCollisions.Where(c => !_visitedStallIds.Contains(c.stall.Id)).ToList();
            if (unseen.Any())
            {
                topCollisions = unseen;
            }

            if (topCollisions.Count > 1)
            {
                // Luật 3: Hướng mặt (Bearing)
                chosen = topCollisions.OrderBy(c => 
                {
                    double bearing = CalculateBearing(userLat, userLon, c.stall.Latitude, c.stall.Longitude);
                    double diff = Math.Abs(bearing - _lastUserHeading);
                    return diff > 180 ? 360 - diff : diff; // ngắn nhất trên vòng tròn 360
                }).First().stall;
            }
            else
            {
                chosen = topCollisions.First().stall;
            }
        }

        if (chosen.Id != _nearestStall?.Id)
        {
            _nearestStall = chosen;
            _visitedStallIds.Add(chosen.Id);

            MainThread.BeginInvokeOnMainThread(async () => 
            {
                UpdateGeofenceBanner(chosen);
                
                // Đồng bộ Highlight lên Bản đồ Leaflet
                RunJsAsync($"if(typeof highlightStall === 'function') highlightStall({_nearestStall?.Id.ToString() ?? "null"});");

                if (chosen != null)
                {
                    if (isPopupOpen) return;
                    isPopupOpen = true;
                    ShowStallPopup(chosen);
                    _ = SpeakCurrentStallAsync(chosen);
                }
            });
        }
    }

    private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double rLat1 = lat1 * Math.PI / 180.0;
        double rLat2 = lat2 * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double y = Math.Sin(dLon) * Math.Cos(rLat2);
        double x = Math.Cos(rLat1) * Math.Sin(rLat2) - Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x);
        bearing = bearing * 180.0 / Math.PI;
        return (bearing + 360) % 360;
    }

    private void UpdateGeofenceBanner(Stall? stall)
    {
        if (stall == null)
        {
            geofenceBanner.IsVisible = false;
            _isTtsPlaying = false;
        }
        else
        {
            lblNearbyStallName.Text = stall.Name;
            btnGeofencePlayAudio.Text = L.Get("map_play_audio");
            btnGeofencePlayAudio.IsEnabled = true;
            geofenceBanner.IsVisible = true;
        }
    }

    private async Task SpeakCurrentStallAsync(Stall stall)
    {
        if (_isTtsPlaying) return;
        _isTtsPlaying = true;
        var watch = Stopwatch.StartNew();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            btnGeofencePlayAudio.Text = "⏳ Processing...";
            btnGeofencePlayAudio.IsEnabled = false;
        });

        try
        {
            var lang = L.CurrentLanguage;
            var textToSpeak = await _audioService.GetStallScriptAsync(stall.Id, lang);
            if (string.IsNullOrWhiteSpace(textToSpeak))
            {
                textToSpeak = !string.IsNullOrWhiteSpace(stall.TtsScript)
                    ? stall.TtsScript
                    : BuildFallback(stall, lang);
            }

            // [MỚI DO AUDIO MANAGER NHẢY VÀO]
            await _audioManager.PlayStallAudioAsync(stall.Id, stall.AudioUrl, textToSpeak);
            Console.WriteLine($"[VOICE_SERVICE] MapPage SpeakAsync triggered for {stall.Name} (Lang: {lang})");
            watch.Stop();
            await SaveListenDurationToLocalAsync(stall, watch.Elapsed);
            await SyncListenDurationAsync(stall.Id, watch.Elapsed);
        }
        catch (Exception ex) { Console.WriteLine($"[VOICE_SERVICE] MapPage Error speaking for {stall.Name}: {ex.Message}"); }
        finally
        {
            _isTtsPlaying = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                btnGeofencePlayAudio.Text = L.Get("map_play_audio");
                btnGeofencePlayAudio.IsEnabled = true;
            });
        }
    }

    private async void OnGeofencePlayAudioClicked(object sender, EventArgs e)
    {
        if (_nearestStall == null || _isTtsPlaying) return;
        await SpeakCurrentStallAsync(_nearestStall);
    }

    private void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop?.Id != 0)
            _ = SpeakCurrentStallAsync(_currentSelectedShop);
    }

    private string BuildFallback(Stall stall, string lang) => lang switch
    {
        "en" => $"Welcome to {stall.Name}! Come enjoy the best street food here.",
        "ja" => $"{stall.Name}へようこそ！最高の屋台料理をお楽しみください。",
        "ko" => $"{stall.Name}에 오신 것을 환영합니다!",
        "zh" => $"欢迎来到{stall.Name}！来享受最好的街头美食。",
        "fr" => $"Bienvenue à {stall.Name} ! Venez savourer la meilleure cuisine de rue.",
        "es" => $"¡Bienvenido a {stall.Name}! Ven a disfrutar de la mejor comida.",
        "de" => $"Willkommen bei {stall.Name}! Genießen Sie das beste Straßenessen.",
        "th" => $"ยินดีต้อนรับสู่ {stall.Name}!",
        "ru" => $"Добро пожаловать в {stall.Name}! Приходите насладиться лучшей уличной едой.",
        _    => $"Chào mừng bạn đến với {stall.Name}! Hãy thưởng thức ẩm thực Vĩnh Khánh."
    };

    // ══════════════════════════════════════════════
    // DATA LOADING
    // ══════════════════════════════════════════════

    private async Task LoadStallsAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lang = L.CurrentLanguage;
            var poiList = await client.GetFromJsonAsync<List<HeriStep.Shared.Models.DTOs.Responses.PointOfInterest>>(
                $"{AppConstants.BaseApiUrl}/api/Stalls?lang={lang}", options);

            if (poiList?.Count > 0)
            {
                _allStalls = poiList.Select(p => new Stall
                {
                    Id = p.Id, Name = p.Name, Latitude = p.Latitude, Longitude = p.Longitude,
                    RadiusMeter = p.RadiusMeter, IsOpen = p.IsOpen, ImageUrl = p.ImageUrl,
                    OwnerId = p.OwnerId, OwnerName = p.OwnerName, TtsScript = p.TtsScript,
                    AudioUrl = p.AudioUrl, Priority = p.Priority
                }).ToList();

                UpdateWebViewStalls();
            }
        }
        catch (Exception ex) 
        { 
            System.Diagnostics.Debug.WriteLine($"[OFFLINE_DB] MapPage stalls fetch failed: {ex.Message}"); 
            try 
            {
                var offlineStalls = await _localDb.GetStallsAsync();
                if (offlineStalls != null && offlineStalls.Count > 0)
                {
                    _allStalls = offlineStalls.Select(ls => new Stall {
                        Id = ls.Id, Name = ls.Name, Latitude = ls.Latitude, Longitude = ls.Longitude,
                        RadiusMeter = (int)ls.RadiusMeter, IsOpen = ls.IsOpen, ImageUrl = ls.ImageUrl, Description = ls.Description,
                        OwnerId = ls.HasOwner ? 1 : null
                    }).ToList();

                    UpdateWebViewStalls();
                    System.Diagnostics.Debug.WriteLine($"[OFFLINE_DB] Map loaded {_allStalls.Count} pins from Local Cache.");
                }
            }
            catch (Exception readEx)
            {
                System.Diagnostics.Debug.WriteLine($"[OFFLINE_DB] MapPage LocalDb fallback failed: {readEx.Message}");
            }
        }
    }

    private void UpdateWebViewStalls()
    {
        if(!_isMapReady || !_isPageActive) return;
        
        var json = JsonSerializer.Serialize(_allStalls.Select(s => new {
            s.Id, s.Name, s.Latitude, s.Longitude, s.IsOpen, s.OwnerId
        }));
        
        // Sử dụng JsonSerializer.Serialize(json) để tự động escape các ký tự đặc biệt, tránh lỗi vỡ câu lệnh JS
        RunJsAsync($"if(typeof updateStalls === 'function') updateStalls({JsonSerializer.Serialize(json)});");
    }

    private void RunJsAsync(string script, bool force = false)
    {
        if (!_isMapReady || !_isPageActive) return;

        // Bỏ qua nếu đang tương tác hoặc vừa mới kết thúc tương tác (trong vòng 500ms)
        // TRỪ KHI là lệnh được ép buộc (force = true) từ các nút bấm UI
        if (!force && (_isUserInteracting || (DateTime.Now - _lastInteractionTime).TotalMilliseconds < 500))
        {
            return;
        }

        // Bỏ qua nếu có quá nhiều lệnh đang thực thi (ngăn tràn bộ nhớ / treo UI Thread)
        // Đây thực chất là non-blocking rate limit
        if (Interlocked.CompareExchange(ref _jsCallsInFlight, 1, 0) != 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try 
            {
                if (_isMapReady && _isPageActive)
                {
                    await mapWebView.EvaluateJavaScriptAsync(script);
                }
            } 
            catch { }
            finally
            {
                Interlocked.Exchange(ref _jsCallsInFlight, 0);
            }
        });
    }

    // ══════════════════════════════════════════════
    // UI EVENTS
    // ══════════════════════════════════════════════

    private async void ClosePopup_Clicked(object sender, EventArgs e)
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        isPopupOpen = false;
        shopPopup.IsVisible = false;
        overlay.IsVisible = false;
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void btnViewDetail_Clicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop?.Id == 0) return;
        ClosePopup();
        try { await Navigation.PushAsync(new ShopDetailPage(_currentSelectedShop, _audioService)); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Nav] {ex.Message}"); }
    }

    private async void btnNavigate_Clicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop == null) return;
        try
        {
            var location = new Microsoft.Maui.Devices.Sensors.Location(_currentSelectedShop.Latitude, _currentSelectedShop.Longitude);
            var options = new MapLaunchOptions { Name = _currentSelectedShop.Name };
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể mở bản đồ chỉ đường: {ex.Message}", "OK");
        }
    }

    private void ToggleLegend_Clicked(object sender, EventArgs e)
        => legendPanel.IsVisible = !legendPanel.IsVisible;

    private async Task SyncListenDurationAsync(int stallId, TimeSpan duration)
    {
        try
        {
            if (stallId <= 0) return;

            var payload = new
            {
                id = Guid.NewGuid(),
                stallId,
                deviceId = GetTrackingDeviceId(),
                visitedAt = DateTime.UtcNow,
                listenDurationSeconds = Math.Max(0, (int)duration.TotalSeconds)
            };

            await _sharedHttpClient.PostAsJsonAsync($"{AppConstants.BaseApiUrl}/api/analytics/stall-visit", payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS_SYNC] SyncListenDurationAsync failed: {ex.Message}");
        }
    }

    private static string GetTrackingDeviceId()
    {
        try
        {
            var id = Preferences.Default.Get("tracking_device_id", string.Empty);
            return string.IsNullOrWhiteSpace(id) ? $"UNKNOWN_{Guid.NewGuid():N}" : id;
        }
        catch
        {
            return $"UNKNOWN_{Guid.NewGuid():N}";
        }
    }

    private async Task SaveListenDurationToLocalAsync(Stall stall, TimeSpan duration)
    {
        try
        {
            var localVisit = new LocalVisitModel
            {
                StallId = stall.Id,
                StallName = stall.Name ?? string.Empty,
                DeviceLat = _lastUserLat,
                DeviceLng = _lastUserLon,
                DistanceMeters = 0,
                VisitedAt = DateTime.UtcNow,
                SessionId = "map-tts",
                ListenDurationSeconds = Math.Max(0, (int)duration.TotalSeconds)
            };

            await _localDb.InsertStallVisitAsync(localVisit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS_SYNC] SaveListenDurationToLocalAsync failed: {ex.Message}");
        }
    }
    private void CenterUser_Clicked(object sender, EventArgs e)
    {
        // Gần như ngay lập tức cập nhật toạ độ và căn giữa (bỏ qua cooldown)
        RunJsAsync($"if(typeof updateUserLocation === 'function') updateUserLocation({_lastUserLat}, {_lastUserLon}, true);", force: true);
        RunJsAsync("if(typeof centerOnUser === 'function') centerOnUser();", force: true);
    }

}
