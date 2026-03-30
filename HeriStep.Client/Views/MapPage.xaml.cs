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

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private Stall _currentSelectedShop = new Stall();
    private WritableLayer? _userLocationLayer;
    private double _userBearingDeg = 0;
    private double _lastUserLat = 10.7595;
    private double _lastUserLon = 106.7025;
    private CancellationTokenSource? _locationLoopCts;

    public MapPage()
    {
        InitializeComponent();

        var map = new Mapsui.Map();
        map.Layers.Add(LocalProxyMapLayer.Create());

        // Layer user location (dưới layer sạp)
        _userLocationLayer = new WritableLayer { Name = "UserLocationLayer", Style = null };
        map.Layers.Add(_userLocationLayer);

        mapView.Map = map;

        // Tắt widget log
        var logW = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "LoggingWidget");
        if (logW != null) logW.Enabled = false;

        // Center Vĩnh Khánh
        var (cx, cy) = SphericalMercator.FromLonLat(106.7025, 10.7595);
        mapView.Map?.Navigator?.CenterOn(new MPoint(cx, cy));
        mapView.Map?.Navigator?.ZoomTo(2.5);

        // Click handler - dùng lambda inline để tránh lỗi EventArgs type
        mapView.Info += (sender, e) => HandleMapInfo(e);
    }

    // ══════════════ LIFECYCLE ══════════════

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ClosePopup();
        await LoadStallsAsync();
        StartUserLocationLoop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationLoopCts?.Cancel();
    }

    // ══════════════ MAP CLICK ══════════════

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
                        : $"http://10.0.2.2:5297{stall.ImageUrl}";
                }

                overlay.IsVisible = true;
                shopPopup.IsVisible = true;
                e.Handled = true;
            }
        }
        else
        {
            ClosePopup();
        }
    }

    // ══════════════ GPS USER LOCATION ══════════════

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
                    var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                    if (loc != null)
                    {
                        _lastUserLat = loc.Latitude;
                        _lastUserLon = loc.Longitude;
                    }
                }
                catch { /* Emulator không có GPS thật → giữ toạ độ mặc định */ }

                // Giả lập bearing (thực tế lấy từ Compass/Accelerometer)
                _userBearingDeg = (_userBearingDeg + 3) % 360;
                UpdateUserLocationOnMap(_lastUserLat, _lastUserLon, _userBearingDeg);

                try { await Task.Delay(2000, token); }
                catch (TaskCanceledException) { break; }
            }
        }, token);
    }

    private void UpdateUserLocationOnMap(double lat, double lon, double bearingDeg)
    {
        if (_userLocationLayer == null) return;

        var (x, y) = SphericalMercator.FromLonLat(lon, lat);

        _userLocationLayer.Clear();

        // ── Vòng xanh nhạt (vùng chính xác GPS, bán kính ~30m) ──
        double accuracyRadius = 40; // meter
        double mPerPixel = 1.0; // at this zoom
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

        // ── Hình nón hướng nhìn (FAN SHAPE bằng chuỗi chấm nhỏ) ──
        double bearingRad = bearingDeg * Math.PI / 180.0;
        double coneAngle = 35 * Math.PI / 180; // ±35° cho nón
        int numDots = 12; // số chấm tạo thành fan
        double maxRadius = 60000; // Đơn vị Mercator (~50m ở zoom này)

        for (int ring = 1; ring <= 3; ring++)
        {
            double ringR = maxRadius * ring / 3.0;
            double dotScale = 0.06 + (0.08 * (3 - ring) / 2.0); // Gần lớn, xa nhỏ
            int alpha = 70 - (ring * 15); // Gần đậm, xa nhạt

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

        // ── Chấm xanh trung tâm (vẽ SAU để nằm TRÊN hình nón) ──
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

    // ══════════════ LOAD STALLS ══════════════

    private async Task LoadStallsAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var url = "http://10.0.2.2:5297/api/Stalls";
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

    // ══════════════ UI HANDLERS ══════════════

    private void ClosePopup_Clicked(object sender, EventArgs e) => ClosePopup();

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

    /// <summary>
    /// NÚT "VỀ VỊ TRÍ HIỆN TẠI" - Center map lên chấm xanh
    /// </summary>
    private void MyLocation_Clicked(object sender, EventArgs e)
    {
        var (mx, my) = SphericalMercator.FromLonLat(_lastUserLon, _lastUserLat);
        mapView.Map?.Navigator?.CenterOn(new MPoint(mx, my));
        mapView.Map?.Navigator?.ZoomTo(2.5);
    }
}