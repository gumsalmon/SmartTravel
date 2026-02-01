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
            // Cập nhật lại tên hàm gọi tại đây
            LoadDataCommand = new Command(async () => await LoadPointsAsync());
        }

        // SỬA: Đổi tên thành LoadPointsAsync để khớp với MainPage.xaml.cs
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
                // LƯU Ý: Nếu BaseAddress đã có "/api/", ở đây chỉ cần gọi "Points"
                var url = $"Points?userLat={lat}&userLon={lon}";

                var data = await _httpClient.GetFromJsonAsync<List<PointOfInterest>>(url);

                if (data != null)
                {
                    // Cập nhật giao diện trên luồng chính
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Points.Clear();
                        foreach (var p in data)
                        {
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