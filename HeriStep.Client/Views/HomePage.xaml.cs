using HeriStep.Client.ViewModels;
using HeriStep.Client.Services;
using System.Linq;
using System.Collections.Generic;
using HeriStep.Shared.Models;

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

        // Subscribe to language changes
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
        if (_langChangedHandler != null)
        {
            L.LanguageChanged -= _langChangedHandler;
            _langChangedHandler = null;
        }
        // Hide suggestions when leaving page
        suggestionPanel.IsVisible = false;
    }

    // ═══════════════════════════════════════════
    // REAL-TIME SEARCH SUGGESTIONS
    // ═══════════════════════════════════════════

    private void OnSearchIconTapped(object sender, EventArgs e)
    {
        // Nhấn icon 🔍 → chỉ focus thanh tìm kiếm (không navigate)
        searchEntry.Focus();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.Trim() ?? "";

        if (string.IsNullOrEmpty(keyword))
        {
            suggestionPanel.IsVisible = false;
            suggestionList.ItemsSource = null;
            return;
        }

        // Search ALL stalls (including those not visible in filtered view)
        // Supports diacritics-insensitive matching: "oc" matches "Ốc", "oanh" matches "Oanh"
        var compareInfo = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
        var suggestions = _viewModel.AllPoints
            .Where(p => p.Name != null &&
                        compareInfo.IndexOf(
                            p.Name, keyword,
                            System.Globalization.CompareOptions.IgnoreNonSpace |
                            System.Globalization.CompareOptions.IgnoreCase) >= 0)
            .Take(6)
            .ToList();

        if (suggestions.Count > 0)
        {
            suggestionList.ItemsSource = suggestions;
            suggestionPanel.IsVisible = true;
        }
        else
        {
            suggestionPanel.IsVisible = false;
        }
    }

    private async void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Stall selectedStall)
        {
            // Hide suggestion, clear text
            suggestionPanel.IsVisible = false;
            searchEntry.Text = "";
            suggestionList.SelectedItem = null;

            // Navigate directly to ShopDetail
            await Navigation.PushAsync(new ShopDetailPage(selectedStall, _audioService));
        }
    }

    // ═══════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════

    private async void OnMapButtonClicked(object sender, EventArgs e)
    {
        // Hide suggestion panel before navigating
        suggestionPanel.IsVisible = false;
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