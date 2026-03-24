using HeriStep.Client.ViewModels;
using System.Linq;
using System.Collections.Generic;

namespace HeriStep.Client.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Chỉ cần gọi API lấy data để hiển thị lên mấy cái thẻ trượt ngang là đủ
        if (_viewModel.Points.Count == 0)
        {
            await _viewModel.LoadPointsAsync();
        }
    }
    // Hàm này sẽ chạy khi User bấm vào nút Bản Đồ
    // Hàm này sẽ chạy khi User bấm vào nút Bản Đồ
    private async void OnMapButtonClicked(object sender, EventArgs e)
    {
        // 💡 ĐÃ FIX: Chỉ cần gọi new MapPage() thôi, không cần xách data theo nữa
        // Vì thằng MapPage bây giờ đã quá xịn, nó tự biết gọi API lấy data rồi!
        await Navigation.PushAsync(new MapPage());
    }
}