using HeriStep.Client.ViewModels;
using HeriStep.Client.Services;
using System.Linq;
using System.Collections.Generic;

namespace HeriStep.Client.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly AudioTranslationService _audioService;
    private Action? _langChangedHandler;

    public HomePage(HomeViewModel viewModel, AudioTranslationService audioService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _audioService = audioService;
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

        // Always reload data
        Console.WriteLine("[LOG] HomePage OnAppearing: reloading data...");
        await _viewModel.LoadPointsAsync();

        // Subscribe to language changes — refresh UI + reload data when language switches
        if (_langChangedHandler == null)
        {
            _langChangedHandler = async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    ApplyLocalization();
                    await _viewModel.LoadPointsAsync();
                });
            };
            L.LanguageChanged += _langChangedHandler;
        }

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        lblTopTours.Text        = L.Get("main_top_tours_title");
        lblTopShops.Text        = L.Get("main_top5_shops_title");
        lblExploreAll.Text      = L.Get("main_explore_all");
        searchEntry.Placeholder = L.Get("search_placeholder");
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

    private async void OnMapButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage(_audioService));
    }

    private async void OnShopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Stall selectedStall)
        {
            var cv = (CollectionView)sender;
            cv.SelectedItem = null;
            await Navigation.PushAsync(new ShopDetailPage(selectedStall, _audioService));
        }
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Tour selectedTour)
        {
            var cv = (CollectionView)sender;
            cv.SelectedItem = null;
            await Navigation.PushAsync(new TourDetailPage(selectedTour));
        }
    }
}