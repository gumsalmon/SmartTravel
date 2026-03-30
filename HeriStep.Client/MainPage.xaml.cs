using System.Net.Http.Json;
using System.Collections.ObjectModel;
using HeriStep.Shared.Models;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<StallViewModel> _stalls = new();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadStallsAsync();
        }

        private async Task LoadStallsAsync()
        {
            loader.IsVisible = true;
            loader.IsRunning = true;

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var url = "http://10.0.2.2:5297/api/Stalls";

                var poiList = await client.GetFromJsonAsync<List<HeriStep.Shared.Models.DTOs.Responses.PointOfInterest>>(url, options);

                _stalls.Clear();
                if (poiList != null && poiList.Count > 0)
                {
                    foreach (var p in poiList)
                    {
                        _stalls.Add(new StallViewModel
                        {
                            Id = p.Id,
                            Name = p.Name ?? "Sạp chưa đặt tên",
                            Latitude = p.Latitude,
                            Longitude = p.Longitude,
                            IsOpen = p.IsOpen,
                            ImageUrl = p.ImageUrl,
                            OwnerId = p.OwnerId,
                            OwnerName = p.OwnerName,
                            RadiusMeter = p.RadiusMeter
                        });
                    }

                    stallList.ItemsSource = _stalls;
                    lblCount.Text = $"{_stalls.Count} cửa hàng tìm thấy";
                    emptyState.IsVisible = false;
                }
                else
                {
                    emptyState.IsVisible = true;
                    lblCount.Text = "Không có dữ liệu";
                }
            }
            catch (Exception ex)
            {
                lblCount.Text = "⚠️ Lỗi kết nối API";
                emptyState.IsVisible = true;
                System.Diagnostics.Debug.WriteLine($"[Store Error] {ex.Message}");
            }
            finally
            {
                loader.IsVisible = false;
                loader.IsRunning = false;
            }
        }

        private async void OnStallTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is StallViewModel vm)
            {
                var stall = new Stall
                {
                    Id = vm.Id,
                    Name = vm.Name,
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,
                    IsOpen = vm.IsOpen,
                    ImageUrl = vm.ImageUrl,
                    OwnerId = vm.OwnerId,
                    OwnerName = vm.OwnerName,
                    RadiusMeter = vm.RadiusMeter
                };

                try
                {
                    await Navigation.PushAsync(new Views.ShopDetailPage(stall));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Nav Error] {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// ViewModel cho danh sách sạp - có thêm các property hiển thị binding
    /// </summary>
    public class StallViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsOpen { get; set; }
        public string? ImageUrl { get; set; }
        public int? OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public int RadiusMeter { get; set; }

        // Computed properties cho XAML binding
        public string DisplayImageUrl =>
            string.IsNullOrEmpty(ImageUrl)
                ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=200"
                : (ImageUrl.StartsWith("http") ? ImageUrl : $"http://10.0.2.2:5297{ImageUrl}");

        public string DisplayOwner =>
            $"👤 {(string.IsNullOrEmpty(OwnerName) ? "Chưa có chủ" : OwnerName)}";

        public string DisplayStatus =>
            !IsOpen ? "⛔ Đã đóng" : (OwnerId == null ? "🟢 Trống" : "🔴 Đang mở");
    }
}