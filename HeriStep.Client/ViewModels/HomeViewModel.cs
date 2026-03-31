using System.Collections.ObjectModel;
using System.Net.Http.Json;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;

namespace HeriStep.Client.ViewModels
{
    public class HomeViewModel : BindableObject
    {
        private readonly HttpClient _httpClient;
        private bool _isBusy;
        private List<Stall> _allPoints = new();

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }

        // List 1: Dành cho khu vực "Tất cả"
        public ObservableCollection<Stall> Points { get; set; } = new();

        // List 2: Dành cho khu vực "Top Quán" (Chờ API bạn của bạn)
        public ObservableCollection<Stall> TopRatedPoints { get; set; } = new();

        // List 3: Dành cho Top 10 Tours
        public ObservableCollection<Tour> TopTours { get; set; } = new();

        private bool _isManualLocation;
        public bool IsManualLocation
        {
            get => _isManualLocation;
            set { if (_isManualLocation != value) { _isManualLocation = value; OnPropertyChanged(); } }
        }

        private string _selectedManualLocation = "Vị trí hiện tại (Auto)";
        public string SelectedManualLocation
        {
            get => _selectedManualLocation;
            set 
            { 
                if (_selectedManualLocation != value) 
                { 
                    _selectedManualLocation = value; 
                    OnPropertyChanged();
                    IsManualLocation = value != "Vị trí hiện tại (Auto)";
                    // Tự động gọi filter hoặc tính toán lại Haversine
                    // LoadPointsAsync().ConfigureAwait(false);
                } 
            }
        }


        public Command LoadDataCommand { get; set; }
        public Command<string> FilterCommand { get; set; }

        private readonly HeriStep.Client.Services.Location.ILocationService _locationService;

        public HomeViewModel(HttpClient httpClient, HeriStep.Client.Services.Location.ILocationService locationService)
        {
            _httpClient = httpClient;
            _locationService = locationService;
            _httpClient.BaseAddress = new Uri(HeriStep.Client.Services.AppConstants.BaseApiUrl);
            string? savedToken = Microsoft.Maui.Storage.Preferences.Default.Get("jwt_token", string.Empty);
            if (!string.IsNullOrEmpty(savedToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", savedToken);
            }

            LoadDataCommand = new Command(async () => await LoadPointsAsync());
            FilterCommand = new Command<string>(FilterAndNavigate);
            
            StartBackgroundGpsLoop();
        }

        private readonly GeofenceService _geofenceService = new();

        private void StartBackgroundGpsLoop()
        {
            // Đăng ký callback khi Radar phát hiện Khách vào gần Sạp
            _geofenceService.StallEntered += async (stall) =>
            {
                string lang = Microsoft.Maui.Storage.Preferences.Default.Get("user_language", "vi");
                string message = lang == "vi"
                    ? $"Chào mừng bạn đến {stall.Name}! Hãy ghé thăm để trải nghiệm ẩm thực Vĩnh Khánh."
                    : $"Welcome to {stall.Name}! Come and experience the best food street.";

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine($"[TTS] Đang phát thanh: {message}");
                    // Phát loa TTS bằng AI giọng nói bản địa của thiết bị
                    await TextToSpeech.Default.SpeakAsync(message);
                });
            };

            Task.Run(async () =>
            {
                while (true)
                {
                    if (!IsManualLocation && _allPoints.Any())
                    {
                        var loc = await _locationService.GetLocationAsync();
                        if (loc != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GPS Radar] Lat: {loc.Latitude:F6}, Lon: {loc.Longitude:F6}");
                            // Bắn thuật toán Haversine kiểm tra khoảng cách đến từng Sạp
                            _geofenceService.CheckProximity(loc.Latitude, loc.Longitude, _allPoints);
                        }
                    }
                    await Task.Delay(5000); // Quét mỗi 5 giây
                }
            });
        }


        public async Task LoadPointsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                string lang = Microsoft.Maui.Storage.Preferences.Default.Get("user_language", "vi");
                var url = $"/api/Stalls?lang={lang}";
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await _httpClient.GetFromJsonAsync<List<Stall>>(url, options);

                if (data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Points.Clear(); TopRatedPoints.Clear(); _allPoints.Clear();
                        foreach (var p in data)
                        {
                            if (!string.IsNullOrEmpty(p.ImageUrl))
                            {
                                if (!p.ImageUrl.StartsWith("http"))
                                    p.ImageUrl = $"{HeriStep.Client.Services.AppConstants.BaseApiUrl}/{p.ImageUrl.TrimStart('/')}";
                            }
                            else
                            {
                                // 💡 BEAUTIFUL FALLBACK: If image is empty, pick one from local resources
                                string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", 
                                                        "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg", 
                                                        "ca_phe_trung.jpg", "com_tam.jpg" };
                                int index = Math.Abs(p.Id) % localFoods.Length;
                                p.ImageUrl = localFoods[index];
                            }

                            Points.Add(p);
                            _allPoints.Add(p);

                            // Tạm thời lấy 5 quán đầu làm Top Rating
                            if (TopRatedPoints.Count < 5) TopRatedPoints.Add(p);
                        }
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
            
            // Load Top Tours song song
            try {
                var toursData = await _httpClient.GetFromJsonAsync<List<Tour>>("/api/Tours/top-hot");
                if (toursData != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TopTours.Clear();
                        foreach (var t in toursData) 
                        {
                            if (string.IsNullOrEmpty(t.ImageUrl))
                            {
                                // 💡 BEAUTIFUL FALLBACK FOR TOURS
                                string[] tourImages = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", "goi_cuon.jpg" };
                                t.ImageUrl = tourImages[Math.Abs(t.Id) % tourImages.Length];
                            }
                            TopTours.Add(t);
                        }
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error TopTours: {ex.Message}"); }

            finally { IsBusy = false; }
        }

        // ==========================================
        // LỌC VÀ CHUYỂN TRANG MỚI (TƯ DUY GRABFOOD)
        // ==========================================
        private async void FilterAndNavigate(string keyword)
        {
            if (_allPoints == null || _allPoints.Count == 0) return;

            // 1. Nếu chọn Tất cả -> Gửi toàn bộ list qua trang mới
            if (string.IsNullOrWhiteSpace(keyword) || keyword == "Tất cả")
            {
                await NavigateToPage(keyword, _allPoints);
                return;
            }

            // 2. Nếu chọn món -> Lọc các quán có chữ đó trong tên
            var filtered = _allPoints.Where(p => p.Name != null && p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            // 3. FALLBACK: ĐỂ BẠN TEST KHÔNG BỊ TRỐNG MÀN HÌNH
            // Nếu lọc chữ "Sò" mà không có quán nào, tự động lấy danh sách gốc để hiện tạm!
            if (filtered.Count == 0)
            {
                filtered = _allPoints;
            }

            await NavigateToPage(keyword, filtered);
        }

        private async Task NavigateToPage(string keyword, List<Stall> dataToPass)
        {
            // Cách gọi chuyển trang "bất bại" trong .NET MAUI từ ViewModel
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                // Dùng MainThread để đảm bảo không bị crash khi chuyển UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.Windows[0].Page.Navigation.PushAsync(new Views.FilterResultPage(keyword, dataToPass));
                });
            }
        }
    }
}