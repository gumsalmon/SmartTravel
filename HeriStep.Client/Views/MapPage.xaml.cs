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
using System;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private Stall _currentSelectedShop;

    // 💡 ĐÃ SỬA: Dùng hàm khởi tạo không tham số để AppShell tự mở được trang này
    public MapPage()
    {
        InitializeComponent();

        // 1. Khởi tạo một đối tượng bản đồ mới
        var map = new Mapsui.Map();

        // 2. Tải lớp hình ảnh đường phố từ OpenStreetMap và đắp vào bản đồ
        map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        // 3. Gán bản đồ vừa tạo vào cái giao diện
        mapView.Map = map;

        var loggingWidget = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "LoggingWidget");
        if (loggingWidget != null)
        {
            loggingWidget.Enabled = false;
        }

        // Tọa độ trung tâm Vĩnh Khánh
        var (x, y) = SphericalMercator.FromLonLat(106.7025, 10.7595);
        mapView.Map?.Navigator?.CenterOn(new MPoint(x, y));
        mapView.Map?.Navigator?.ZoomTo(2.5);

        // BÍ QUYẾT SENIOR (CHUẨN MAPSUI V5): Bắt sự kiện click vào ghim
        mapView.Info += (sender, e) =>
        {
            var layers = mapView.Map?.Layers;
            if (layers == null) return;

            var mapInfo = e.GetMapInfo(layers);

            if (mapInfo?.Feature != null)
            {
                var clickedPoint = mapInfo.Feature["PointData"] as Stall;

                if (clickedPoint != null)
                {
                    _currentSelectedShop = clickedPoint;

                    // Bơm dữ liệu vào thẻ Bottom Sheet
                    popupName.Text = clickedPoint.Name;

                    if (!string.IsNullOrEmpty(clickedPoint.ImageUrl))
                    {
                        // 💡 ĐÃ FIX LỖI ẢNH TÀNG HÌNH: Tự động nối thêm IP của máy chủ
                        string fullImageUrl = clickedPoint.ImageUrl.StartsWith("http")
                            ? clickedPoint.ImageUrl
                            : $"http://10.0.2.2:5297{clickedPoint.ImageUrl}";

                        popupImage.Source = fullImageUrl;
                    }

                    // Hiện thẻ Popup lên
                    overlay.IsVisible = true;
                    shopPopup.IsVisible = true;

                    e.Handled = true; // Ngăn bản đồ zoom/pan khi đang bấm vào ghim
                }
            }
            else
            {
                ClosePopup();
            }
        };
    }

    // ==========================================
    // 💡 THẦN DƯỢC MAUI: Tự động gọi API tải sạp mỗi khi mở trang Bản Đồ
    // ==========================================
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            using var client = new HttpClient();

            // LƯU Ý: Nhớ đổi 10.0.2.2 thành IP máy tính (VD: 192.168.1.45) nếu test trên điện thoại thật!
            var url = "http://10.0.2.2:5297/api/Stalls";
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Kéo dữ liệu từ Backend của sếp về
            var freshData = await client.GetFromJsonAsync<List<Stall>>(url, options);

            if (freshData != null && freshData.Count > 0)
            {
                // Xóa lớp ghim cũ trên bản đồ để vẽ lại từ đầu (tránh đè ghim)
                var existingLayer = mapView.Map?.Layers.FirstOrDefault(l => l.Name == "QuanOcLayer");
                if (existingLayer != null)
                {
                    mapView.Map?.Layers.Remove(existingLayer);
                }

                // Cắm ghim mới nhất lên bản đồ
                DrawPinsOnMap(freshData);
                mapView.RefreshGraphics();
                System.Diagnostics.Debug.WriteLine($"[Thành công] Đã cắm {freshData.Count} sạp lên bản đồ!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lỗi Bản Đồ]: {ex.Message}");
            // Báo lỗi lên màn hình nếu rớt mạng API
            await DisplayAlert("Lỗi tải bản đồ", "Không thể lấy danh sách sạp. Vui lòng kiểm tra lại mạng hoặc API!", "OK");
        }
    }

    private void DrawPinsOnMap(IEnumerable<Stall> points)
    {
        var features = new List<PointFeature>();

        foreach (var point in points)
        {
            // Bỏ qua các sạp lỗi tọa độ (0, 0) để không bị bay ra Châu Phi
            if (point.Latitude == 0 && point.Longitude == 0) continue;

            var (x, y) = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);

            // Giấu dữ liệu quán vào ghim
            var feature = new PointFeature(new MPoint(x, y))
            {
                ["Name"] = point.Name,
                ["PointData"] = point
            };

            feature.Styles.Clear();

            // 💡 Tuyệt chiêu: Sạp có chủ màu Đỏ, Sạp trống màu Xanh lá
            var pinColor = point.OwnerId == null
                ? new Mapsui.Styles.Color(40, 167, 69) // Xanh lá
                : new Mapsui.Styles.Color(220, 53, 69); // Đỏ

            var dotStyle = new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.4,
                Fill = new Mapsui.Styles.Brush(pinColor),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
            };

            var labelStyle = new LabelStyle
            {
                Text = point.Name ?? "Sạp chưa đặt tên",
                Font = new Mapsui.Styles.Font { Size = 13, Bold = true },
                BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Transparent),
                ForeColor = pinColor,
                Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                Offset = new Offset(12, 0)
            };

            feature.Styles.Add(dotStyle);
            feature.Styles.Add(labelStyle);
            features.Add(feature);
        }

        var pinLayer = new MemoryLayer
        {
            Name = "QuanOcLayer",
            Features = features,
            Style = null
        };

        mapView.Map?.Layers.Add(pinLayer);
    }

    private void ClosePopup_Clicked(object sender, EventArgs e)
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        overlay.IsVisible = false;
        shopPopup.IsVisible = false;
    }

    private async void btnViewDetail_Clicked(object sender, EventArgs e)
    {
        if (_currentSelectedShop != null)
        {
            await Navigation.PushAsync(new ShopDetailPage(_currentSelectedShop));
        }
    }
}