using System.Collections.ObjectModel;
using HeriStep.Shared;
using System.Net.Http.Json;
using System.Diagnostics;

namespace HeriStep.Client.ViewModels
{
    public class HomeViewModel : BindableObject
    {
        private readonly HttpClient _httpClient;
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<PointOfInterest> Points { get; set; } = new();

        public Command LoadDataCommand { get; set; }

        public HomeViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            LoadDataCommand = new Command(async () => await LoadPointsAsync());
        }

        public async Task LoadPointsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // 1. LẤY TỌA ĐỘ GPS
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
                    GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                double lat = location?.Latitude ?? 0;
                double lon = location?.Longitude ?? 0;

                // 2. GỌI API
                var url = $"Points?userLat={lat}&userLon={lon}";
                var data = await _httpClient.GetFromJsonAsync<List<PointOfInterest>>(url);

                if (data != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Points.Clear();
                        foreach (var p in data)
                        {
                            // --- PHẦN SỬA QUAN TRỌNG ĐỂ HIỆN ẢNH ---
                            // Nếu ImageUrl chưa có "http", ta nối thêm địa chỉ IP máy tính vào
                            if (!string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.StartsWith("http"))
                            {
                                // 10.0.2.2 là IP để máy ảo Android truy cập vào Localhost của bạn
                                p.ImageUrl = $"http://10.0.2.2:5297/images/{p.ImageUrl}";
                            }
                            // ---------------------------------------

                            Points.Add(p);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HeriStep Error]: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}