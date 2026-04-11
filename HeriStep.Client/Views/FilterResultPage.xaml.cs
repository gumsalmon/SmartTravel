using HeriStep.Shared.Models;
using System.Collections.ObjectModel;
using HeriStep.Client.Services;

namespace HeriStep.Client.Views;

public partial class FilterResultPage : ContentPage
{
    public string CategoryTitle { get; set; }
    public ObservableCollection<Stall> FilteredPoints { get; set; }

    private readonly AudioTranslationService _audioService;

    // Hàm khởi tạo nhận vào Tên Danh Mục và Danh sách quán đã lọc
    public FilterResultPage(string categoryName, List<Stall> points, AudioTranslationService audioService)
    {
        InitializeComponent();
        _audioService = audioService;

        CategoryTitle = $"Kết quả tìm kiếm: {categoryName}";

        // Đưa dữ liệu vào Collection
        FilteredPoints = new ObservableCollection<Stall>(points);

        BindingContext = this;
    }

    // Sự kiện khi người dùng BẤM VÀO 1 QUÁN TRONG DANH SÁCH DỌC
    private async void OnShopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Stall selectedShop)
        {
            // Bỏ highlight chỗ vừa click để lần sau click lại vẫn ăn
            ((CollectionView)sender).SelectedItem = null;

            // BAY SANG TRANG CHI TIẾT CỦA BẠN!
            await Navigation.PushAsync(new ShopDetailPage(selectedShop, _audioService));
        }
    }
}