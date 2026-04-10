using HeriStep.Client.ViewModels;
using HeriStep.Client.Services;
using System.Linq;
using System.Collections.Generic;

namespace HeriStep.Client.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private Action? _langChangedHandler;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // GPS permission check
#if !WINDOWS
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
#endif

        // Always reload data (to pick up language changes, new stalls, etc.)
        Console.WriteLine("[LOG] HomePage OnAppearing: reloading data...");
        await _viewModel.LoadPointsAsync();

        // Subscribe to language changes — reload data when language switches
        if (_langChangedHandler == null)
        {
            _langChangedHandler = async () =>
            {
                Console.WriteLine("[LOG] Language changed, reloading data...");
                await MainThread.InvokeOnMainThreadAsync(async () => await _viewModel.LoadPointsAsync());
            };
            L.LanguageChanged += _langChangedHandler;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe to prevent memory leak from static event
        if (_langChangedHandler != null)
        {
            L.LanguageChanged -= _langChangedHandler;
            _langChangedHandler = null;
        }
    }
    // Hàm này sẽ chạy khi User bấm vào nút Bản Đồ
    // Hàm này sẽ chạy khi User bấm vào nút Bản Đồ
    private async void OnMapButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    private async void OnShopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Stall selectedStall)
        {
            // Reset selection
            var cv = (CollectionView)sender;
            cv.SelectedItem = null;

            await Navigation.PushAsync(new ShopDetailPage(selectedStall));
        }
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Tour selectedTour)
        {
            var cv = (CollectionView)sender;
            cv.SelectedItem = null;

            await DisplayAlert("Tour Khám Phá", $"Bạn đã chọn: {selectedTour.TourName}. Lịch trình chi tiết đang được phát triển.", "OK");
            // Optionally, could go to MapPage to show all stalls on that tour.
            await Navigation.PushAsync(new MapPage());
        }
    }
}