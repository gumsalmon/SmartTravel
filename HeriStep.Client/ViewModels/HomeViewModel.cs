using System.Collections.ObjectModel;
using HeriStep.Shared;
using System.Net.Http.Json;

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

        public Command LoadDataCommand { get; set; }
        public Command<string> FilterCommand { get; set; }

        public HomeViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            LoadDataCommand = new Command(async () => await LoadPointsAsync());
            FilterCommand = new Command<string>(FilterAndNavigate);
        }

        public async Task LoadPointsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                double lat = 10.7595; double lon = 106.7025;
                var url = $"api/Points?userLat={lat}&userLon={lon}";
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await _httpClient.GetFromJsonAsync<List<Stall>>(url, options);

                if (data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Points.Clear(); TopRatedPoints.Clear(); _allPoints.Clear();
                        foreach (var p in data)
                        {
                            if (!string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.StartsWith("http"))
                                p.ImageUrl = $"http://172.21.8.215:5297/images/{p.ImageUrl}";

                            Points.Add(p);
                            _allPoints.Add(p);

                            // Tạm thời lấy 5 quán đầu làm Top Rating cho giao diện đẹp
                            if (TopRatedPoints.Count < 5) TopRatedPoints.Add(p);
                        }
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
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
            if (Application.Current?.MainPage != null)
            {
                // Dùng MainThread để đảm bảo không bị crash khi chuyển UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new Views.FilterResultPage(keyword, dataToPass));
                });
            }
        }
    }
}