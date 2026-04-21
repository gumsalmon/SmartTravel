using HeriStep.Client.ViewModels;
using HeriStep.Client.Services;
using System.Linq;
using System.Collections.Generic;
using HeriStep.Shared.Models;
using System.Globalization;
using System.Text;

namespace HeriStep.Client.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly AudioTranslationService _audioService;
    private readonly AudioManagerService _audioManager;
    private readonly LocalDatabaseService _localDb;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly LocationTrackingService _trackingService;
    private Action? _langChangedHandler;
    private Action<bool>? _connectivityHandler;

    public HomePage(HomeViewModel viewModel, AudioTranslationService audioService,
                    AudioManagerService audioManager, LocalDatabaseService localDb,
                    GeofenceEngine geofenceEngine, LocationTrackingService trackingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _audioService = audioService;
        _audioManager = audioManager;
        _localDb = localDb;
        _geofenceEngine = geofenceEngine;
        _trackingService = trackingService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ── GPS permission ────────────────────────────────────────────────────
#if !WINDOWS
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
#endif

        // ── Show offline banner immediately if no internet ────────────────────
        UpdateOfflineBanner(!ConnectivityService.IsOnline);

        // ── Subscribe to connectivity changes ──────────────────────────────
        if (_connectivityHandler == null)
        {
            _connectivityHandler = (isOnline) =>
            {
                UpdateOfflineBanner(!isOnline);
            };
            ConnectivityService.ConnectivityChanged += _connectivityHandler;
        }

        // ── Data ─────────────────────────────────────────────────────────────
        Console.WriteLine("[LOG] HomePage OnAppearing: reloading data...");
        await _viewModel.LoadPointsAsync();

        // ── Language changes ─────────────────────────────────────────────────
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_langChangedHandler != null)
        {
            L.LanguageChanged -= _langChangedHandler;
            _langChangedHandler = null;
        }

        if (_connectivityHandler != null)
        {
            ConnectivityService.ConnectivityChanged -= _connectivityHandler;
            _connectivityHandler = null;
        }

        suggestionPanel.IsVisible = false;
    }

    // ═══════════════════════════════════════════
    // OFFLINE BANNER
    // ═══════════════════════════════════════════
    private void UpdateOfflineBanner(bool showOffline)
    {
        offlineBanner.IsVisible = showOffline;
        if (showOffline)
            lblOffline.Text = L.Get("offline_banner");
        else
            lblOffline.Text = L.Get("online_banner");
    }

    // ═══════════════════════════════════════════
    // LOCALIZATION  (#4 — language consistency fix)
    // ═══════════════════════════════════════════
    private void ApplyLocalization()
    {
        lblTopTours.Text            = L.Get("main_top_tours_title");
        lblTopShops.Text            = L.Get("main_top5_shops_title");
        lblExploreAll.Text          = L.Get("main_explore_all");
        // Fixes Chinese placeholder bug — always use L.Get
        searchEntry.Placeholder     = L.Get("search_placeholder");
        lblEmptyTours.Text          = L.Get("empty_tours");
        lblEmptyShops.Text          = L.Get("empty_shops");
        lblEmptySearch.Text         = L.Get("empty_search");
        lblOffline.Text             = ConnectivityService.IsOnline
                                      ? L.Get("online_banner")
                                      : L.Get("offline_banner");
    }

    // ═══════════════════════════════════════════
    // REAL-TIME SEARCH SUGGESTIONS
    // ═══════════════════════════════════════════

    private void OnSearchIconTapped(object sender, EventArgs e) => searchEntry.Focus();

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.Trim() ?? "";

        if (string.IsNullOrEmpty(keyword))
        {
            suggestionPanel.IsVisible = false;
            suggestionList.ItemsSource = null;
            return;
        }

        var suggestions = _viewModel.AllPoints
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) &&
                        NormalizeSearch(p.Name).Contains(NormalizeSearch(keyword),
                                                          StringComparison.OrdinalIgnoreCase))
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
            suggestionPanel.IsVisible = false;
            searchEntry.Text = "";
            suggestionList.SelectedItem = null;
            await Navigation.PushAsync(new ShopDetailPage(selectedStall, _audioService));
        }
    }

    // ═══════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════

    private async void OnMapButtonClicked(object sender, EventArgs e)
    {
        suggestionPanel.IsVisible = false;
        await Navigation.PushAsync(new MapPage(_audioService, _audioManager,
                                               _localDb, _geofenceEngine, _trackingService));
    }

    private async void OnShopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Stall selectedStall)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new ShopDetailPage(selectedStall, _audioService));
        }
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Tour selectedTour)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new TourDetailPage(selectedTour));
        }
    }

    // ═══════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════

    private static string NormalizeSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .ToLowerInvariant();
    }
}