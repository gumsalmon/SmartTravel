using HeriStep.Client.ViewModels;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        // Sử dụng private field để dễ dàng gọi lại ViewModel ở các hàm khác
        private readonly HomeViewModel _viewModel;

        // SỬA: Inject trực tiếp HomeViewModel thay vì HttpClient
        public MainPage(HomeViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            // Gắn bộ não cho giao diện
            BindingContext = _viewModel;
        }

        // QUAN TRỌNG: Hàm này sẽ tự chạy mỗi khi bạn mở hoặc quay lại trang này
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Gọi lệnh nạp 10 sạp hàng từ API
            if (_viewModel != null)
            {
                await _viewModel.LoadPointsAsync();
            }
        }
    }
}