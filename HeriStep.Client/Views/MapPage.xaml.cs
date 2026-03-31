using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Layers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;
using System;
using System.Threading;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private Stall _currentSelectedShop = new Stall();
    private WritableLayer? _userLocationLayer;
    private double _userBearingDeg = 0;
    private double _lastUserLat = 10.7595;
    private double _lastUserLon = 106.7025;
    private CancellationTokenSource? _locationLoopCts;
    private List<Stall> _allStalls = new();
    private List<int> _nearbyStallIds = new();

    // ========== DEMO MODE SETTINGS ==========
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

    public MapPage()
    {
        InitializeComponent();

        var map = new Mapsui.Map();
        map.Layers.Add(LocalProxyMapLayer.Create());

        _userLocationLayer = new WritableLayer { Name = "UserLocationLayer", Style = null };
        map.Layers.Add(_userLocationLayer);

        mapView.Map = map;

        var logW = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "LoggingWidget");
        if (logW != null) logW.Enabled = false;

        var (cx, cy) = SphericalMercator.FromLonLat(106.7025, 10.7595);
        mapView.Map?.Navigator?.CenterOn(new MPoint(cx, cy));
        mapView.Map?.Navigator?.ZoomTo(2.5);

        mapView.Info += (sender, e) => HandleMapInfo(e);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ClosePopup();
        await LoadStallsAsync();
        StartUserLocationLoop();

        // 💡 FORCED INIT: Silent speak to force Android TTS engine to wake up early.
        _ = Task.Run(async () => {
            try { 
                await TextToSpeech.Default.GetLocalesAsync(); 
                await TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });
            } catch { }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationLoopCts?.Cancel();
    }

    private void HandleMapInfo(dynamic e)
    {
        var layers = mapView.Map?.Layers;
        if (layers == null) return;

        var mapInfo = e.GetMapInfo(layers);

        if (mapInfo?.Feature != null)
        {
            var stall = mapInfo.Feature["PointData"] as Stall;
            if (stall != null)
            {
                ShowStallPopup(stall); // 💡 Use refactored helper
                e.Handled = true;
            }
        }
        else
        {
            ClosePopup();
        }
    }

    // 💡 NEW HELPER: Shows the info popup for a selected stall
    private void ShowStallPopup(Stall stall)
    {
        if (stall == null) return;
        
        _currentSelectedShop = stall;
        popupName.Text = stall.Name;
        lblOwner.Text = $"👤 {(string.IsNullOrEmpty(stall.OwnerName) ? "Chưa có chủ" : stall.OwnerName)}";

        if (!stall.IsOpen)
        {
            lblStatusTag.Text = "⛔ Đã đóng";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#EF4444");
        }
        else if (stall.OwnerId == null)
        {
            lblStatusTag.Text = "🟢 Trống";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
        }
        else
        {
            lblStatusTag.Text = "🔴 Mở";
            lblStatusTag.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#EF4444");
        }

        if (!string.IsNullOrEmpty(stall.ImageUrl))
        {
            popupImage.Source = stall.ImageUrl.StartsWith("http")
                ? stall.ImageUrl
                : $"{HeriStep.Client.Services.AppConstants.BaseApiUrl}{stall.ImageUrl}";
        }
        else
        {
            // 💡 BEAUTIFUL FALLBACK: If image is empty, pick one from local resources
            string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", 
                                    "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg", 
                                    "ca_phe_trung.jpg", "com_tam.jpg" };
            int index = Math.Abs(stall.Id) % localFoods.Length;
            popupImage.Source = localFoods[index];
        }

        overlay.IsVisible = true;
        shopPopup.IsVisible = true;
    }

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
                        var nextPoint = _demoPath[_demoPathIndex];
                        _lastUserLat = nextPoint.Latitude;
                        _lastUserLon = nextPoint.Longitude;
                        _demoPathIndex = (_demoPathIndex + 1) % _demoPath.Count;
                    }
                    else
                    {
                        var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                        if (loc != null)
                        {
                            _lastUserLat = loc.Latitude;
                            _lastUserLon = loc.Longitude;
                        }
                    }
                }
                catch { }

                _userBearingDeg = (_userBearingDeg + 3) % 360;
                UpdateUserLocationOnMap(_lastUserLat, _lastUserLon, _userBearingDeg);
                CheckNearbyStalls(_lastUserLat, _lastUserLon);

                int delay = _isDemoMode ? 1000 : 2000;
                try { await Task.Delay(delay, token); }
                catch (TaskCanceledException) { break; }
            }
        }, token);
    }

    private void CheckNearbyStalls(double userLat, double userLon)
    {
        var radius = Preferences.Default.Get("voice_radius", 50.0);
        var userLoc = new Location(userLat, userLon);

        var currentNearby = _allStalls.Where(s => {
            var stallLoc = new Location(s.Latitude, s.Longitude);
            return Location.CalculateDistance(userLoc, stallLoc, DistanceUnits.Kilometers) * 1000 <= radius;
        }).ToList();

        var newIds = currentNearby.Select(s => s.Id).OrderBy(id => id).ToList();
        
        if (!newIds.SequenceEqual(_nearbyStallIds))
        {
            _nearbyStallIds = newIds;
            MainThread.BeginInvokeOnMainThread(() => UpdateNearbyStallsPanel(currentNearby));
        }
    }

    private void UpdateNearbyStallsPanel(List<Stall> stalls)
    {
        nearbyStallsList.Clear();
        if (stalls.Count == 0)
        {
            nearbyScroll.IsVisible = false;
            return;
        }

        foreach (var stall in stalls)
        {
            var btn = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#EE1A1A2E"),
                Padding = new Thickness(12, 6),
                StrokeThickness = 1,
                Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#4285F4"),
                Content = new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = {
                        new Label { Text = "🔊", VerticalOptions = LayoutOptions.Center },
                        new Label { Text = stall.Name, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }
                    }
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => {
                ShowStallPopup(stall); // 💡 Show popup immediately
                PlayStallAudio(stall); // 💡 Then play audio
            };
            btn.GestureRecognizers.Add(tap);

            nearbyStallsList.Add(btn);
        }

        nearbyScroll.IsVisible = true;
    }

    private async void PlayStallAudio(Stall stall)
    {
        try
        {
            var lang = Preferences.Default.Get("user_language", "vi");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var url = $"{HeriStep.Client.Services.AppConstants.BaseApiUrl}/api/Stalls/{stall.Id}/tts/{lang}";
            
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TtsResponse>();
                if (data != null && !string.IsNullOrEmpty(data.Text))
                {
                    var settings = new SpeechOptions 
                    { 
                        Pitch = 1.0f, 
                        Volume = 1.0f,
                        Locale = (await TextToSpeech.Default.GetLocalesAsync()).FirstOrDefault(l => l.Language.StartsWith(lang))
                    };
                    await TextToSpeech.Default.SpeakAsync(data.Text, settings);
                }
            }
            else
            {
                string fallback = lang switch {
                    "en" => $"Introduction about {stall.Name}",
                    "ja" => $"{stall.Name}についての紹介",
                    "ko" => $"{stall.Name}에 대한 소개",
                    "zh" => $"关于 {stall.Name} 的介绍",
                    "fr" => $"Introduction sur {stall.Name}",
                    "es" => $"Introducción sobre {stall.Name}",
                    "de" => $"Einführung über {stall.Name}",
                    "th" => $"ข้อมูลแนะนำเกี่ยวกับ {stall.Name}",
                    _ => $"Giới thiệu về sạp {stall.Name}"
                };

                var settings = new SpeechOptions 
                { 
                    Locale = (await TextToSpeech.Default.GetLocalesAsync()).FirstOrDefault(l => l.Language.StartsWith(lang))
                };
                await TextToSpeech.Default.SpeakAsync(fallback, settings);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio Error] {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể phát giới thiệu: {ex.Message}", "OK");
        }
    }

    public class TtsResponse { public string Text { get; set; } = ""; }

    private void UpdateUserLocationOnMap(double lat, double lon, double bearingDeg)
    {
        if (_userLocationLayer == null) return;

        var (x, y) = SphericalMercator.FromLonLat(lon, lat);

        _userLocationLayer.Clear();

        double accuracyRadius = 40; 
        double mPerPixel = 1.0; 
        double resolution = mapView.Map?.Navigator?.Viewport.Resolution ?? 1;
        double radiusPx = accuracyRadius / Math.Max(resolution, 0.1);

        var accuracyFeature = new PointFeature(new MPoint(x, y));
        accuracyFeature.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = Math.Max(0.4, radiusPx / 60.0),
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(66, 133, 244, 30)),
            Outline = new Mapsui.Styles.Pen(new Mapsui.Styles.Color(66, 133, 244, 60), 1)
        });

        double bearingRad = bearingDeg * Math.PI / 180.0;
        double coneAngle = 35 * Math.PI / 180; 
        int numDots = 12; 
        double maxRadius = 60000; 

        for (int ring = 1; ring <= 3; ring++)
        {
            double ringR = maxRadius * ring / 3.0;
            double dotScale = 0.06 + (0.08 * (3 - ring) / 2.0); 
            int alpha = 70 - (ring * 15); 

            for (int i = 0; i <= numDots; i++)
            {
                double angle = bearingRad - coneAngle + (2 * coneAngle * i / numDots);
                double dx = ringR * Math.Sin(angle);
                double dy = ringR * Math.Cos(angle);

                var conePoint = new PointFeature(new MPoint(x + dx, y + dy));
                conePoint.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = dotScale,
                    Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(66, 133, 244, alpha))
                });
                _userLocationLayer.Add(conePoint);
            }
        }

        var dotFeature = new PointFeature(new MPoint(x, y));
        dotFeature.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.35,
            Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(66, 133, 244)),
            Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3)
        });

        _userLocationLayer.Add(accuracyFeature);
        _userLocationLayer.Add(dotFeature);

        MainThread.BeginInvokeOnMainThread(() => mapView.RefreshGraphics());
    }

    private async Task LoadStallsAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var url = $"{HeriStep.Client.Services.AppConstants.BaseApiUrl}/api/Stalls";
            var poiList = await client.GetFromJsonAsync<List<HeriStep.Shared.Models.DTOs.Responses.PointOfInterest>>(url, options);

            if (poiList != null && poiList.Count > 0)
            {
                var stalls = poiList.Select(p => new Stall
                {
                    Id = p.Id,
                    Name = p.Name,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    RadiusMeter = p.RadiusMeter,
                    IsOpen = p.IsOpen,
                    ImageUrl = p.ImageUrl,
                    OwnerId = p.OwnerId,
                    OwnerName = p.OwnerName
                }).ToList();

                _allStalls = stalls;
                var old = mapView.Map?.Layers.FirstOrDefault(l => l.Name == "QuanOcLayer");
                if (old != null) mapView.Map?.Layers.Remove(old);

                DrawPinsOnMap(stalls);
                mapView.RefreshGraphics();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Map Error] {ex.Message}");
        }
    }

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
            string stallLabel = point.Name ?? "Sạp";

            if (!point.IsOpen)
            {
                pinColor = new Mapsui.Styles.Color(150, 150, 160);
                stallLabel += " [Đóng]";
            }
            else if (point.OwnerId == null)
            {
                pinColor = new Mapsui.Styles.Color(40, 167, 69);
            }
            else
            {
                pinColor = new Mapsui.Styles.Color(220, 53, 69);
            }

            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.4,
                Fill = new Mapsui.Styles.Brush(pinColor),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
            });

            feature.Styles.Add(new LabelStyle
            {
                Text = stallLabel,
                Font = new Mapsui.Styles.Font { Size = 12, Bold = true },
                BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Transparent),
                ForeColor = pinColor,
                Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3),
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

    private void ClosePopup_Clicked(object sender, EventArgs e) => ClosePopup();

    private void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop != null && _currentSelectedShop.Id != 0)
        {
            PlayStallAudio(_currentSelectedShop);
        }
    }

    private void ClosePopup()
    {
        overlay.IsVisible = false;
        shopPopup.IsVisible = false;
    }

    private async void btnViewDetail_Clicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop == null || _currentSelectedShop.Id == 0) return;
        ClosePopup();
        try
        {
            await Navigation.PushAsync(new ShopDetailPage(_currentSelectedShop));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nav Error] {ex.Message}");
        }
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ToggleLegend_Clicked(object sender, EventArgs e)
    {
        legendPanel.IsVisible = !legendPanel.IsVisible;
    }

    private void MyLocation_Clicked(object sender, EventArgs e)
    {
        _isDemoMode = false;
        btnDemoMode.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#EE1A1A2E");
        CenterMapOnUser();
    }

    private void CenterMapOnUser()
    {
        var (mx, my) = SphericalMercator.FromLonLat(_lastUserLon, _lastUserLat);
        mapView.Map?.Navigator?.CenterOn(new MPoint(mx, my));
        mapView.Map?.Navigator?.ZoomTo(2.5);
    }

    private void DemoMode_Clicked(object sender, EventArgs e)
    {
        _isDemoMode = !_isDemoMode;
        if (_isDemoMode)
        {
            _demoPathIndex = 0;
            btnDemoMode.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#4285F4");
        }
        else
        {
            btnDemoMode.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#EE1A1A2E");
        }
    }
}
