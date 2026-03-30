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

#if !WINDOWS
        // Xin quyền GPS theo bài toán MVPs (Windows Unpackaged sẽ tự động Crash nếu gọi hàm này, nên bỏ qua)
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
#endif

        // Chọn ngôn ngữ nếu chưa có
        if (!Microsoft.Maui.Storage.Preferences.Default.ContainsKey("lang_code"))
        {
            string action = await DisplayActionSheet("Chọn Ngôn Ngữ / Select Language", "Cancel", null, "Tiếng Việt", "English", "한국어", "日本語");
            string langCode = action switch { "English" => "en", "한국어" => "ko", "日本語" => "ja", _ => "vi" };
            Microsoft.Maui.Storage.Preferences.Default.Set("lang_code", langCode);
        }

        // Gọi API với ngôn ngữ (query lang có thể bỏ vào LoadPointsAsync sau)
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