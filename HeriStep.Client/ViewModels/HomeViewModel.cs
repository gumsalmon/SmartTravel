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

        // Danh sách hiển thị lên giao diện
        public ObservableCollection<PointOfInterest> Points { get; set; } = new();

        public Command LoadDataCommand { get; set; }

        public HomeViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Lệnh làm mới (Refresh)
            LoadDataCommand = new Command(async () => await LoadPoints());
        }

        public async Task LoadPoints()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // 1. LẤY TỌA ĐỘ GPS (Yêu cầu cốt lõi của đồ án SGU)
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
                    GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                double lat = location?.Latitude ?? 0;
                double lon = location?.Longitude ?? 0;

                // 2. GỌI API KÈM TỌA ĐỘ (Để Server lọc cửa hàng gần nhất)
                // Chú ý: Route "api/Points" phải khớp với Controller ở Backend
                var url = $"api/Points?userLat={lat}&userLon={lon}";
                var data = await _httpClient.GetFromJsonAsync<List<PointOfInterest>>(url);

                if (data != null)
                {
                    Points.Clear();
                    foreach (var p in data)
                    {
                        Points.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HeriStep Error]: {ex.Message}");
                // Có thể dùng Shell.Current.DisplayAlert để báo lỗi cho người dùng
            }
            finally
            {
                IsBusy = false; // Luôn tắt vòng xoay dù thành công hay thất bại
            }
        }
    }
}