using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Layers;
using System.Collections.Generic;
using System.Linq;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private Stall _currentSelectedShop;
    public MapPage(IEnumerable<Stall> points)
    {
        InitializeComponent();
        // 1. Khởi tạo một đối tượng bản đồ mới
        var map = new Mapsui.Map();

        // 2. Tải lớp hình ảnh đường phố từ OpenStreetMap và đắp vào bản đồ
        map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        // 3. Gán bản đồ vừa tạo vào cái giao diện (Lưu ý: mapView là tên của cái bản đồ bên file .xaml)
        mapView.Map = map;

        mapView.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());

        var loggingWidget = mapView.Map?.Widgets.FirstOrDefault(w => w.GetType().Name == "LoggingWidget");
        if (loggingWidget != null)
        {
            loggingWidget.Enabled = false;
        }

        var (x, y) = SphericalMercator.FromLonLat(106.7025, 10.7595);
        mapView.Map?.Navigator?.CenterOn(new MPoint(x, y));
        mapView.Map?.Navigator?.ZoomTo(2.5);

        DrawPinsOnMap(points);

        // ==========================================
        // BÍ QUYẾT SENIOR: Dùng Lambda (=>) để C# tự động hiểu dữ liệu Click
        // ==========================================
        // ==========================================
        // BÍ QUYẾT SENIOR: Gọi thẳng e.Feature theo chuẩn Mapsui mới nhất!
        // ==========================================
        // ==========================================
        // BÍ QUYẾT SENIOR (CHUẨN MAPSUI V5): 
        // Phải dùng hàm e.GetMapInfo(layers) để "đào" dữ liệu lên!
        // ==========================================
        mapView.Info += (sender, e) =>
        {
            var layers = mapView.Map?.Layers;
            if (layers == null) return;

            // 1. Dùng hàm GetMapInfo() rà quét tất cả các lớp bản đồ
            var mapInfo = e.GetMapInfo(layers);

            // 2. Nếu quét trúng một cái Ghim (Feature)
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
                        popupImage.Source = clickedPoint.ImageUrl;
                    }

                    // Hiện thẻ Popup lêns
                    overlay.IsVisible = true;
                    shopPopup.IsVisible = true;

                    e.Handled = true; // Ngăn bản đồ zoom/pan khi đang bấm vào ghim
                }
            }
            else
            {
                // 3. Nếu bấm ra ngoài đường trống -> Đóng popup
                ClosePopup();
            }
        };
    }

    private void DrawPinsOnMap(IEnumerable<Stall> points)
    {
        var features = new List<PointFeature>();

        foreach (var point in points)
        {
            var (x, y) = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);

            // Giấu dữ liệu quán vào ghim
            var feature = new PointFeature(new MPoint(x, y))
            {
                ["Name"] = point.Name,
                ["PointData"] = point
            };

            feature.Styles.Clear();

            var dotStyle = new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.4,
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(26, 115, 232)),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
            };

            var labelStyle = new LabelStyle
            {
                Text = point.Name,
                Font = new Mapsui.Styles.Font { Size = 13, Bold = true },
                BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Transparent),
                ForeColor = new Mapsui.Styles.Color(26, 115, 232),
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
            // TUYỆT ĐỐI KHÔNG dùng IsMapInfoLayer ở đây nữa nhé!
        };

        mapView.Map?.Layers.Add(pinLayer);
    }

    // Sự kiện khi bấm vào vùng đen hoặc nút [X]
    private void ClosePopup_Clicked(object sender, EventArgs e)
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        overlay.IsVisible = false;
        shopPopup.IsVisible = false;
    }
    // Xử lý khi bấm nút "Xem Chi Tiết Quán"
    private async void btnViewDetail_Clicked(object sender, EventArgs e)
    {
        // Nếu trong túi có dữ liệu quán
        if (_currentSelectedShop != null)
        {
            // Vút sang trang mới và ném dữ liệu quán đó qua!
            await Navigation.PushAsync(new ShopDetailPage(_currentSelectedShop));
        }
    }
}
