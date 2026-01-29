using HeriStep.Client.ViewModels;

namespace HeriStep.Client.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();

        // Gán ViewModel vào biến private để sử dụng trong OnAppearing
        _viewModel = viewModel;

        // Cầu nối kết nối UI (XAML) với Logic (ViewModel)
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Chỉ tải dữ liệu nếu danh sách hiện tại đang trống 
        // Điều này giúp tiết kiệm 4G/Pin cho khách du lịch khi sử dụng app
        if (_viewModel.Points.Count == 0)
        {
            await _viewModel.LoadPoints();
        }
    }
}