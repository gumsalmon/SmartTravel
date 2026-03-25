using HeriStep.Shared;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using System.Collections.Generic;
using System.Linq;

namespace HeriStep.Client.Views;

public partial class MapPage : ContentPage
{
    private IDispatcherTimer _gpsTimer;
    private int _lastSpokenStallId = -1;
    private Stall _currentSelectedShop;
    private IEnumerable<Stall> _stalls;
    public MapPage(IEnumerable<Stall> points)
    {
        InitializeComponent();
        // _stalls = points;
        _stalls = new List<Stall>
        {
            new Stall { Id = 999, Name = "Ốc Oanh Test", Latitude = 10.7601, Longitude = 106.7025 }
        };
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
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Gọi hàm xin quyền và bật Timer quét GPS
        await StartGPSRadar();
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
    private async Task StartGPSRadar()
    {
        // 1. Xin quyền GPS từ người dùng
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        // Nếu khách không cho quyền thì chịu, tắt radar
        if (status != PermissionStatus.Granted)
            return;

        // 2. Khởi động Timer quét mỗi 5 giây
        _gpsTimer = Dispatcher.CreateTimer();
        _gpsTimer.Interval = TimeSpan.FromSeconds(5);
        _gpsTimer.Tick += async (s, e) => await ScanLocationAndCheckStalls();
        _gpsTimer.Start();
    }
    private async Task ScanLocationAndCheckStalls()
    {
        try
        {
            // 1. Lấy tọa độ hiện tại của khách (Độ chính xác cao nhất)
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));
            var userLocation = await Geolocation.Default.GetLocationAsync(request);

            if (userLocation != null)
            {
                // 2. VẼ CHẤM XANH LÊN BẢN ĐỒ MAPSUI
                // Giả sử cái bản đồ trong file XAML của bạn tên là "mapControl" (x:Name="mapControl")
                // Code này sẽ update vị trí chấm xanh (My Location) liên tục
                //var myPosition = new Mapsui.UI.Maui.Position(userLocation.Latitude, userLocation.Longitude);
                //mapControl.MyLocationLayer.UpdateMyLocation(myPosition);

                // 3. GỌI HÀM KIỂM TRA BÁN KÍNH 15M (Sẽ viết ở Bước 3)
                await CheckGeofencingAndSpeak(userLocation);
            }
        }
        catch (Exception ex)
        {
            // Khách tắt GPS giữa chừng hoặc lỗi mạng
            Console.WriteLine($"Lỗi GPS: {ex.Message}");
        }
    }
    private async Task CheckGeofencingAndSpeak(Location userLocation)
    {
        // Giả sử _stalls là danh sách các sạp bạn đã lấy từ API về
        // Nếu biến danh sách sạp của bạn tên khác thì đổi lại nhé
        foreach (var stall in _stalls)
        {
            // Tính khoảng cách (Trả về đơn vị Kilometers, nên phải nhân 1000 ra Mét)
            double distanceInMeters = Location.CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                stall.Latitude, stall.Longitude,
                DistanceUnits.Kilometers) * 1000;

            // Nếu khách bước vào vùng 15 mét quanh quán
            if (distanceInMeters <= 5000)
            {
                // Kiểm tra xem đã đọc loa quán này chưa (tránh đọc lặp lại 1 quán hoài)
                if (_lastSpokenStallId != stall.Id)
                {
                    _lastSpokenStallId = stall.Id; // Đánh dấu là đã đọc

                    // Đoạn text muốn đọc (Bạn có thể lấy từ trường TTS_Script của DB hoặc tự ghép)
                    string promoText = $"Chào mừng bạn đến với {stall.Name}. Hôm nay chúng tôi có khuyến mãi đặc biệt!";

                    // Gọi loa
                    await SpeakToUser(promoText);
                }

                break; // Tìm thấy 1 quán gần nhất là thoát vòng lặp, chờ 5s sau quét tiếp
            }
        }
    }

    private async Task SpeakToUser(string text)
    {
        // Tùy chỉnh giọng đọc (tốc độ, âm lượng)
        SpeechOptions options = new SpeechOptions()
        {
            Volume = 1.0f, // Max volume
            Pitch = 1.0f   // Giọng bình thường
        };

        // Lệnh phát âm thanh của MAUI (Nó sẽ tự dùng giọng của Google/Siri trên điện thoại)
        await TextToSpeech.Default.SpeakAsync(text, options);
    }
    private async void btnChiDuong_Clicked(object sender, EventArgs e)
    {
        // Giả sử bạn lấy được tọa độ quán mà khách vừa bấm vào
        double stallLat = 10.7601; // Thay bằng stall.Latitude thực tế
        double stallLng = 106.7025; // Thay bằng stall.Longitude thực tế
        string stallName = "Ốc Oanh"; // Thay bằng stall.Name thực tế

        var targetLocation = new Location(stallLat, stallLng);

        // Cấu hình mở bản đồ với chế độ Đi bộ (Walking)
        var options = new MapLaunchOptions
        {
            Name = stallName,
            NavigationMode = NavigationMode.Walking
        };

        try
        {
            // Ra lệnh cho hệ điều hành mở App Bản đồ mặc định
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(targetLocation, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường trên máy này.", "OK");
        }
    }
}
