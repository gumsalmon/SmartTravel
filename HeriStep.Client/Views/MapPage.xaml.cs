using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Layers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;
using HeriStep.Client.Services.Location;
using System;
using System.Threading;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private bool isPopupOpen = false;
    // ═══ STATE ═══
    private Stall _currentSelectedShop = new Stall();
    private Stall? _nearestStall = null;
    private WritableLayer? _userLocationLayer;
    private double _userBearingDeg = 0;
    private double _lastUserLat = 10.7595;
    private double _lastUserLon = 106.7025;
    private CancellationTokenSource? _locationLoopCts;
    private List<Stall> _allStalls = new();
    private bool _isTtsPlaying = false;
    private readonly AudioTranslationService _audioService;
    private readonly LocalDatabaseService _localDb;
    private readonly GeofenceEngine _botGeofenceEngine;

    // ═══ FREE EXPLORATION MODE ═══
    private bool _isFreeExploreMode = false;
    private CancellationTokenSource? _freeExploreCts;


    // ═══ DEMO MODE ═══
    private bool _isDemoMode = false;
    private int _demoPathIndex = 0;
    private readonly List<Location> _demoPath = new()
    {
        new Location(10.7588, 106.7008),
        new Location(10.7592, 106.7015),
        new Location(10.7595, 106.7025),
        new Location(10.7598, 106.7032),
        new Location(10.7602, 106.7038),
        new Location(10.7610, 106.7042),
        new Location(10.7602, 106.7038),
        new Location(10.7598, 106.7032),
        new Location(10.7595, 106.7025)
    };

    // Mapsui 5: ZoomTo takes resolution (m/px), ~2.4 ≈ OSM zoom 16
    private const double StreetLevelResolution = 2.4;
    private bool _isBotTestMode = false;
    private IDispatcherTimer? _botTimer;
    private Stall? _botTargetStall;
    private const double BotStepMeters = 2.5;
    private volatile bool _isPageActive;

    // ══════════════════════════════════════════════
    // Savory Ember — amber pin for matching stalls
    // ══════════════════════════════════════════════
    private static readonly Mapsui.Styles.Color PinAmber     = new(211, 84,   0);   // #D35400
    private static readonly Mapsui.Styles.Color PinGreen     = new(40,  167, 69);
    private static readonly Mapsui.Styles.Color PinGrey      = new(107, 91,  78);
    private static readonly Mapsui.Styles.Color PinHighlight = new(255, 191,  0);   // #FFBF00

    public MapPage(AudioTranslationService audioService, LocalDatabaseService localDb, GeofenceEngine geofenceEngine)
    {
        InitializeComponent();
        _audioService = audioService;
        _localDb = localDb;
        _botGeofenceEngine = geofenceEngine;

        var map = new Mapsui.Map();
        map.Layers.Add(LocalProxyMapLayer.Create());

        _userLocationLayer = new WritableLayer { Name = "UserLocationLayer", Style = null };
        map.Layers.Add(_userLocationLayer);

        mapView.Map = map;

        var logW = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "LoggingWidget");
        if (logW != null) logW.Enabled = false;

        // Move Zoom controls to bottom-left to avoid search bar overlap
        var zoomWInfo = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "ZoomInOutWidget");
        if (zoomWInfo != null)
        {
            dynamic z = zoomWInfo;
            z.HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left;
            z.VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom;
            z.MarginX = 16f;
            z.MarginY = 120f; // Offset above bottom tabs safely
        }

        // ✅ FIX: CenterOnVinhKhanh() moved to OnAppearing — viewport must be ready first
        mapView.Info += (sender, e) => HandleMapInfo(e);
    }

    private void CenterOnVinhKhanh()
    {
        var (cx, cy) = SphericalMercator.FromLonLat(106.7025, 10.7595);
        mapView.Map?.Navigator?.CenterOn(new MPoint(cx, cy));
        mapView.Map?.Navigator?.ZoomTo(StreetLevelResolution);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isPageActive = true;
        ClosePopup();
        ApplyLocalization();

        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try { CenterOnVinhKhanh(); }
                catch (Exception ex) { Console.WriteLine($"[ERROR] CenterOnVinhKhanh failed: {ex.Message}"); }
            });
        });

        await LoadStallsAsync();
        StartUserLocationLoop();

        _ = _audioService.WarmUpAsync();
    }

    private void ApplyLocalization()
    {
        // Map popup & banner labels (localized)
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
        StopFreeExplore();
        StopBotTest();
    }



    // ══════════════════════════════════════════════
    // MAP TAP
    // ══════════════════════════════════════════════

    private void HandleMapInfo(dynamic e)
    {
        var layers = mapView.Map?.Layers;
        if (layers == null) return;

        var mapInfo = e.GetMapInfo(layers);
        if (mapInfo?.Feature != null)
        {
            var stall = mapInfo.Feature["PointData"] as Stall;
            if (stall != null) { ShowStallPopup(stall); e.Handled = true; }
        }
        else { ClosePopup(); }
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
                try
                {
                    if (_isDemoMode)
                    {
                        var next = _demoPath[_demoPathIndex];
                        _lastUserLat = next.Latitude;
                        _lastUserLon = next.Longitude;
                        _demoPathIndex = (_demoPathIndex + 1) % _demoPath.Count;
                    }
                    else if (_isBotTestMode)
                    {
                        // Bot đang điều khiển tọa độ giả lập, giữ nguyên _lastUserLat/_lastUserLon.
                    }
                    else
                    {
                        var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                        if (loc != null) { _lastUserLat = loc.Latitude; _lastUserLon = loc.Longitude; }
                    }
                }
                catch { }

                _userBearingDeg = (_userBearingDeg + 3) % 360;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isPageActive) return;
                    UpdateUserLocationOnMap(_lastUserLat, _lastUserLon, _userBearingDeg);
                });
                CheckNearbyStalls(_lastUserLat, _lastUserLon);

                int delay = (_isDemoMode || _isBotTestMode) ? 1000 : 2000;
                try { await Task.Delay(delay, token); }
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

        Stall? nearest = null;
        double shortest = double.MaxValue;

        foreach (var stall in _allStalls)
        {
            if (stall.Latitude == 0 && stall.Longitude == 0) continue;

            var stallLoc = new Microsoft.Maui.Devices.Sensors.Location(stall.Latitude, stall.Longitude);
            double dist = Microsoft.Maui.Devices.Sensors.Location
                .CalculateDistance(userLoc, stallLoc, DistanceUnits.Kilometers) * 1000;

            double r = stall.RadiusMeter > 0 ? Math.Min(stall.RadiusMeter, globalRadius) : globalRadius;

            if (dist <= r && dist < shortest) { shortest = dist; nearest = stall; }
        }

        if (nearest?.Id != _nearestStall?.Id)
        {
            _nearestStall = nearest;
            MainThread.BeginInvokeOnMainThread(() => 
            {
                UpdateGeofenceBanner(nearest);
                if (nearest != null)
                {
                    if (isPopupOpen) return;
                    isPopupOpen = true;
                    ShowStallPopup(nearest);
                    _ = SpeakCurrentStallAsync(nearest);
                }
            });
        }
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

            await _audioService.SpeakAsync(textToSpeak, lang);
            Console.WriteLine($"[VOICE_SERVICE] MapPage SpeakAsync triggered for {stall.Name} (Lang: {lang})");
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

    // ══════════════════════════════════════════════
    // TTS — FETCHED FROM StallContents (DB)
    // ══════════════════════════════════════════════

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
    // USER LOCATION RENDERING (Savory Ember colours)
    // ══════════════════════════════════════════════

    private void UpdateUserLocationOnMap(double lat, double lon, double bearingDeg)
    {
        if (_userLocationLayer == null) return;

        var (x, y) = SphericalMercator.FromLonLat(lon, lat);
        _userLocationLayer.Clear();

        // Accuracy circle — amber tint
        double resolution = mapView.Map?.Navigator?.Viewport.Resolution ?? 1;
        double radiusPx = 40.0 / Math.Max(resolution, 0.1);

        var accFeature = new PointFeature(new MPoint(x, y));
        accFeature.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = Math.Max(0.4, radiusPx / 60.0),
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(211, 84, 0, 25)),
            Outline = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(211, 84, 0, 60), 1)
        });

        // Direction cone (amber dots)
        double bearingRad = bearingDeg * Math.PI / 180.0;
        double coneAngle = 32 * Math.PI / 180;
        for (int ring = 1; ring <= 3; ring++)
        {
            double ringR = 55000 * ring / 3.0;
            double dotScale = 0.05 + 0.07 * (3 - ring) / 2.0;
            int alpha = 65 - ring * 15;
            for (int i = 0; i <= 10; i++)
            {
                double angle = bearingRad - coneAngle + 2 * coneAngle * i / 10;
                var cp = new PointFeature(new MPoint(x + ringR * Math.Sin(angle), y + ringR * Math.Cos(angle)));
                cp.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = dotScale,
                    Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 191, 0, alpha))  // amber
                });
                _userLocationLayer.Add(cp);
            }
        }

        // Central dot — amber-gold
        var dot = new PointFeature(new MPoint(x, y));
        dot.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.35,
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(211, 84, 0)),
            Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3)
        });

        _userLocationLayer.Add(accFeature);
        _userLocationLayer.Add(dot);
        MainThread.BeginInvokeOnMainThread(() => mapView.RefreshGraphics());
    }

    // ══════════════════════════════════════════════
    // DATA LOADING
    // ══════════════════════════════════════════════

    private async Task LoadStallsAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lang = L.CurrentLanguage;
            var poiList = await client.GetFromJsonAsync<List<HeriStep.Shared.Models.DTOs.Responses.PointOfInterest>>(
                $"{AppConstants.BaseApiUrl}/api/Stalls?lang={lang}", options);

            if (poiList?.Count > 0)
            {
                _allStalls = poiList.Select(p => new Stall
                {
                    Id = p.Id, Name = p.Name, Latitude = p.Latitude, Longitude = p.Longitude,
                    RadiusMeter = p.RadiusMeter, IsOpen = p.IsOpen, ImageUrl = p.ImageUrl,
                    OwnerId = p.OwnerId, OwnerName = p.OwnerName, TtsScript = p.TtsScript
                }).ToList();

                var old = mapView.Map?.Layers.FirstOrDefault(l => l.Name == "QuanOcLayer");
                if (old != null) mapView.Map?.Layers.Remove(old);
                DrawPinsOnMap(_allStalls);
                mapView.RefreshGraphics();
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

                    var old = mapView.Map?.Layers.FirstOrDefault(l => l.Name == "QuanOcLayer");
                    if (old != null) mapView.Map?.Layers.Remove(old);
                    DrawPinsOnMap(_allStalls);
                    MainThread.BeginInvokeOnMainThread(() => mapView.RefreshGraphics());
                    System.Diagnostics.Debug.WriteLine($"[OFFLINE_DB] Map loaded {_allStalls.Count} pins from Local Cache.");
                }
            }
            catch (Exception readEx)
            {
                System.Diagnostics.Debug.WriteLine($"[OFFLINE_DB] MapPage LocalDb fallback failed: {readEx.Message}");
            }
        }
    }

    /// <summary>
    /// Draws stall pins. If search query is active, matching stalls use amber highlight (#FFBF00),
    /// non-matching are dimmed with opacity. Mapsui layers use memory features only — no WebView.
    /// </summary>
    private void DrawPinsOnMap(IEnumerable<Stall> points)
    {
        var features = new List<PointFeature>();

        foreach (var point in points)
        {
            if (point.Latitude == 0 && point.Longitude == 0) continue;

            var (x, y) = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
            var feature = new PointFeature(new MPoint(x, y))
            {
                ["Name"] = point.Name,
                ["PointData"] = point
            };
            feature.Styles.Clear();

            Mapsui.Styles.Color pinColor;
            string label = point.Name ?? "Sạp";

            if (!point.IsOpen)
            {
                pinColor = PinGrey;
                label += " [Đóng]";
            }
            else if (point.OwnerId == null) pinColor = PinGreen;
            else pinColor = PinAmber;

            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.42,
                Fill = new Mapsui.Styles.Brush(pinColor),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
            });

            feature.Styles.Add(new LabelStyle
            {
                Text = label,
                Font = new Mapsui.Styles.Font { Size = 11, Bold = true },
                BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Transparent),
                ForeColor = pinColor,
                Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.Black, 2),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                Offset = new Offset(12, 0)
            });

            features.Add(feature);
        }

        mapView.Map?.Layers.Add(new MemoryLayer
        {
            Name = "QuanOcLayer",
            Features = features,
            Style = null
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

    private void MyLocation_Clicked(object sender, EventArgs e)
    {
        _isDemoMode = false;
        btnDemoMode.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B5B4E");
        CenterMapOnUser();
    }

    private void CenterMapOnUser()
    {
        var (mx, my) = SphericalMercator.FromLonLat(_lastUserLon, _lastUserLat);
        mapView.Map?.Navigator?.CenterOn(new MPoint(mx, my));
        mapView.Map?.Navigator?.ZoomTo(StreetLevelResolution);
    }

    private void DemoMode_Clicked(object sender, EventArgs e)
    {
        _isDemoMode = !_isDemoMode;
        if (_isDemoMode)
        {
            _demoPathIndex = 0;
            _nearestStall = null;
            geofenceBanner.IsVisible = false;
            btnDemoMode.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#D35400");
        }
        else btnDemoMode.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B5B4E");
    }

    private async void BotTest_Clicked(object sender, EventArgs e)
    {
        _isBotTestMode = !_isBotTestMode;
        if (_isBotTestMode)
        {
            await StartBotTestAsync();
        }
        else
        {
            StopBotTest();
        }
    }

    private async Task StartBotTestAsync()
    {
        if (_allStalls.Count == 0)
        {
            await LoadStallsAsync();
        }

        _botTargetStall = _allStalls
            .Where(s => s.Latitude != 0 || s.Longitude != 0)
            .OrderBy(s => Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(
                new Microsoft.Maui.Devices.Sensors.Location(_lastUserLat, _lastUserLon),
                new Microsoft.Maui.Devices.Sensors.Location(s.Latitude, s.Longitude),
                DistanceUnits.Kilometers))
            .FirstOrDefault();

        if (_botTargetStall == null)
        {
            await DisplayAlert("Bot Test", "Không tìm thấy quán hợp lệ trong dữ liệu.", "OK");
            _isBotTestMode = false;
            return;
        }

        btnBotTest.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFBF00");
        lblFreeExploreStatus.Text = $"🤖 Bot → {_botTargetStall.Name}";
        freeExploreBanner.IsVisible = true;

        _botTimer?.Stop();
        _botTimer = Application.Current!.Dispatcher.CreateTimer();
        _botTimer.Interval = TimeSpan.FromSeconds(1.5);
        _botTimer.Tick += async (_, _) =>
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await BotStepAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOT_TEST] Timer tick failed: {ex.Message}");
            }
        };
        _botTimer.Start();
    }

    private async Task BotStepAsync()
    {
        try
        {
            if (!_isBotTestMode || _botTargetStall == null)
            {
                return;
            }

            var current = new Microsoft.Maui.Devices.Sensors.Location(_lastUserLat, _lastUserLon);
            var target = new Microsoft.Maui.Devices.Sensors.Location(_botTargetStall.Latitude, _botTargetStall.Longitude);
            var distanceMeters = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(current, target, DistanceUnits.Kilometers) * 1000;
            if (distanceMeters <= 1.0)
            {
                return;
            }

            var ratio = Math.Min(BotStepMeters / Math.Max(distanceMeters, 0.1), 1.0);
            _lastUserLat = _lastUserLat + ((_botTargetStall.Latitude - _lastUserLat) * ratio);
            _lastUserLon = _lastUserLon + ((_botTargetStall.Longitude - _lastUserLon) * ratio);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (!_isPageActive) return;
                    UpdateUserLocationOnMap(_lastUserLat, _lastUserLon, _userBearingDeg);
                    CheckNearbyStalls(_lastUserLat, _lastUserLon);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BOT_TEST] UI update failed: {ex.Message}");
                }
            });

            try
            {
                await _botGeofenceEngine.InjectLocationAsync(_lastUserLat, _lastUserLon);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BOT_TEST] InjectLocation failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BOT_TEST] BotStepAsync failed: {ex.Message}");
        }
    }

    private void StopBotTest()
    {
        _isBotTestMode = false;
        if (_botTimer != null)
        {
            _botTimer.Stop();
            _botTimer = null;
        }
        btnBotTest.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#6B5B4E");
    }

    // ══════════════════════════════════════════════
    // CHẾ ĐỘ KHÁM PHÁ TỰ DO (FREE EXPLORATION MODE)
    // ══════════════════════════════════════════════
    // Luồng theo sơ đồ sequence Hình 29:
    // 1. Nạp TẤT CẢ tọa độ sạp từ SQLite vào RAM
    // 2. Loop nền mỗi 5 giây: lấy GPS → Haversine → check <=20m
    // 3. Nếu IsVisited=false → phát TTS → đánh cờ IsVisited=true
    // ══════════════════════════════════════════════

    private async void FreeExplore_Clicked(object sender, EventArgs e)
    {
        _isFreeExploreMode = !_isFreeExploreMode;

        if (_isFreeExploreMode)
        {
            // Reset cờ IsVisited để bắt đầu phiên khám phá mới
            await _localDb.ResetAllVisitedAsync();

            btnFreeExplore.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
            freeExploreBanner.IsVisible = true;
            lblFreeExploreStatus.Text = L.Get("map_free_explore_on");

            StartFreeExploreLoop();
        }
        else
        {
            StopFreeExplore();
        }
    }

    private void StartFreeExploreLoop()
    {
        _freeExploreCts?.Cancel();
        _freeExploreCts = new CancellationTokenSource();
        var token = _freeExploreCts.Token;

        Task.Run(async () =>
        {
            // Bước 1: Nạp TẤT CẢ tọa độ sạp từ SQLite Local vào RAM
            var allLocalStalls = await _localDb.GetStallsAsync();
            Console.WriteLine($"[FREE_EXPLORE] Loaded {allLocalStalls.Count} stalls from SQLite into RAM.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Bước 2: Lấy GPS hiện tại mỗi 5 giây
                    double lat = _lastUserLat;
                    double lon = _lastUserLon;

                    try
                    {
                        var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                        if (loc != null) { lat = loc.Latitude; lon = loc.Longitude; }
                    }
                    catch { }

                    // Bước 3: Thuật toán Haversine — tìm sạp trong vùng <=20m chưa nghe
                    var userLoc = new Microsoft.Maui.Devices.Sensors.Location(lat, lon);

                    foreach (var ls in allLocalStalls)
                    {
                        if (ls.Latitude == 0 && ls.Longitude == 0) continue;
                        if (ls.IsVisited) continue; // Đã nghe → bỏ qua

                        var stallLoc = new Microsoft.Maui.Devices.Sensors.Location(ls.Latitude, ls.Longitude);
                        double distMeters = Microsoft.Maui.Devices.Sensors.Location
                            .CalculateDistance(userLoc, stallLoc, DistanceUnits.Kilometers) * 1000;

                        double radius = ls.RadiusMeter > 5 ? Math.Min(ls.RadiusMeter, 20) : 20;

                        if (distMeters <= radius)
                        {
                            Console.WriteLine($"[FREE_EXPLORE] Entered zone of '{ls.Name}' ({distMeters:F1}m). Playing TTS...");

                            // Bước 4: Truy xuất kịch bản TtsScript theo ngôn ngữ đã chọn
                            string script = await _audioService.GetStallScriptAsync(ls.Id, L.CurrentLanguage)
                                            ?? ls.TtsScript
                                            ?? BuildFallback(new Stall { Name = ls.Name }, L.CurrentLanguage);

                            // Đánh cờ IsVisited = true TRƯỚC khi phát để tránh vòng lặp
                            ls.IsVisited = true;
                            await _localDb.MarkStallVisitedAsync(ls.Id);

                            // Cập nhật banner
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                lblFreeExploreStatus.Text = $"🔊 {ls.Name}";
                                lblNearbyStallName.Text = ls.Name;
                                geofenceBanner.IsVisible = true;
                                btnGeofencePlayAudio.IsEnabled = false;
                            });

                            // Bước 5: Phát TTS bằng giọng bản địa
                            await _audioService.SpeakAsync(script, L.CurrentLanguage);

                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                lblFreeExploreStatus.Text = L.Get("map_free_explore_on");
                                btnGeofencePlayAudio.IsEnabled = true;
                            });

                            break; // Chỉ phát 1 sạp mỗi lần quét
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FREE_EXPLORE] Loop error: {ex.Message}");
                }

                // Scan mỗi 5 giây theo sơ đồ sequence
                try { await Task.Delay(5000, token); }
                catch (TaskCanceledException) { break; }
            }

            Console.WriteLine("[FREE_EXPLORE] Loop stopped.");
        }, token);
    }

    private void StopFreeExplore()
    {
        _isFreeExploreMode = false;
        _freeExploreCts?.Cancel();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            freeExploreBanner.IsVisible = false;
            btnFreeExplore.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
        });
    }

}
