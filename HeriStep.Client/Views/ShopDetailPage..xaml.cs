using HeriStep.Shared;

namespace HeriStep.Client.Views;

public partial class ShopDetailPage : ContentPage
{
    // Bắt buộc phải có 1 biến Stall truyền vào đây
    public ShopDetailPage(Stall shop)
    {
        InitializeComponent();

        // Ẩn cái thanh Navigation mặc định xấu xí đi
        NavigationPage.SetHasNavigationBar(this, false);

        // PHÉP THUẬT DATA BINDING: Gắn toàn bộ dữ liệu của quán vào Giao Diện!
        BindingContext = shop;
    }

    // Nút quay lại
    // Nút quay lại "Bất tử" chuẩn MAUI
    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        // Thử lùi về bằng Navigation mặc định
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else
        {
            // Nếu bị kẹt, dùng Shell để ép lùi về
            await Shell.Current.GoToAsync("..");
        }
    }
}