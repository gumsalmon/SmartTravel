using System.Collections.ObjectModel;
using HeriStep.Shared;
using System.Windows.Input;
using System.Net.Http.Json;

namespace HeriStep.Client.ViewModels
{
    public class HomeViewModel
    {
        private readonly HttpClient _httpClient;

        // Đây là cái danh sách sẽ hiện lên màn hình điện thoại
        public ObservableCollection<PointOfInterest> Points { get; set; } = new ObservableCollection<PointOfInterest>();

        // Lệnh để kéo xuống làm mới trang
        public Command LoadDataCommand { get; set; }
        public bool IsBusy { get; set; } // Trạng thái đang tải

        public HomeViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
            LoadDataCommand = new Command(async () => await LoadPoints());

            // Tự động tải dữ liệu ngay khi mở app
            Task.Run(LoadPoints);
        }

        public async Task LoadPoints()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Gọi lên Server lấy danh sách về
                var data = await _httpClient.GetFromJsonAsync<List<PointOfInterest>>("api/Points");

                if (data != null)
                {
                    Points.Clear(); // Xóa dữ liệu cũ
                    foreach (var p in data)
                    {
                        Points.Add(p); // Thêm dữ liệu mới
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi rồi: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}