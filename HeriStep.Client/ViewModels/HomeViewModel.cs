using System.Collections.ObjectModel;
using System.Net.Http.Json;
using HeriStep.Shared.Models;
using HeriStep.Client.Services;

namespace HeriStep.Client.ViewModels
{
    /// <summary>
    /// ViewModel for the Home/Explore page. Loads stall data from the API,
    /// manages the background GPS geofencing loop, and provides filtered
    /// navigation to sub-pages.
    /// </summary>
    public class HomeViewModel : BindableObject
    {
        private readonly HttpClient _httpClient;
        private readonly GeofenceService _geofenceService;
        private bool _isBusy;
        private List<Stall> _allPoints = new();

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }

        /// <summary>All stalls for the main list.</summary>
        public ObservableCollection<Stall> Points { get; set; } = new();

        /// <summary>Top-rated stalls section.</summary>
        public ObservableCollection<Stall> TopRatedPoints { get; set; } = new();

        /// <summary>Top hot tours.</summary>
        public ObservableCollection<Tour> TopTours { get; set; } = new();

        public Command LoadDataCommand { get; set; }
        public Command<string> FilterCommand { get; set; }

        private readonly HeriStep.Client.Services.Location.ILocationService _locationService;

        public HomeViewModel(HttpClient httpClient, HeriStep.Client.Services.Location.ILocationService locationService)
        {
            _httpClient = httpClient;
            _locationService = locationService;
            _httpClient.BaseAddress ??= new Uri(AppConstants.BaseApiUrl);
            _geofenceService = new GeofenceService();

            // Apply JWT token if available
            string? savedToken = Preferences.Default.Get("jwt_token", string.Empty);
            if (!string.IsNullOrEmpty(savedToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", savedToken);
            }

            LoadDataCommand = new Command(async () => await LoadPointsAsync());
            FilterCommand = new Command<string>(FilterAndNavigate);

            StartBackgroundGpsLoop();
        }

        /// <summary>
        /// Starts a background loop that monitors the user's GPS position
        /// and triggers TTS announcements via the GeofenceService when
        /// entering a stall's proximity zone.
        /// </summary>
        private void StartBackgroundGpsLoop()
        {
            _geofenceService.StallEntered += async (stall) =>
            {
                string lang = L.CurrentLanguage;
                string message = lang switch
                {
                    "en" => $"Welcome to {stall.Name}! Come enjoy the best street food here.",
                    "ja" => $"{stall.Name}へようこそ！",
                    "ko" => $"{stall.Name}에 오신 것을 환영합니다!",
                    "zh" => $"欢迎来到{stall.Name}！",
                    "fr" => $"Bienvenue à {stall.Name} !",
                    "es" => $"¡Bienvenido a {stall.Name}!",
                    "de" => $"Willkommen bei {stall.Name}!",
                    "th" => $"ยินดีต้อนรับสู่ {stall.Name}!",
                    _ => $"Chào mừng bạn đến {stall.Name}! Hãy ghé thăm để trải nghiệm ẩm thực Vĩnh Khánh."
                };

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine($"[TTS] Playing: {message}");
                    try
                    {
                        var locales = await TextToSpeech.Default.GetLocalesAsync();
                        var locale = locales?.FirstOrDefault(l => l.Language.StartsWith(lang));
                        await TextToSpeech.Default.SpeakAsync(message, new SpeechOptions { Locale = locale });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TTS Error] {ex.Message}");
                    }
                });
            };

            Task.Run(async () =>
            {
                while (true)
                {
                    if (_allPoints.Any())
                    {
                        var loc = await _locationService.GetLocationAsync();
                        if (loc != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[GPS Radar] Lat: {loc.Latitude:F6}, Lon: {loc.Longitude:F6}");
                            _geofenceService.CheckProximity(loc.Latitude, loc.Longitude, _allPoints);
                        }
                    }
                    await Task.Delay(5000); // Scan every 5 seconds
                }
            });
        }

        /// <summary>
        /// Loads all stalls and tours from the API, applying the current language.
        /// </summary>
        public async Task LoadPointsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                string lang = L.CurrentLanguage;
                var url = $"/api/Stalls?lang={lang}";
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Console.WriteLine($"[LOG] Fetching stalls from: {AppConstants.BaseApiUrl}{url}");
                var data = await _httpClient.GetFromJsonAsync<List<Stall>>(url, options);

                if (data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Points.Clear(); _allPoints.Clear();
                        foreach (var p in data)
                        {
                            if (!string.IsNullOrEmpty(p.ImageUrl))
                            {
                                if (!p.ImageUrl.StartsWith("http"))
                                    p.ImageUrl = $"{AppConstants.BaseApiUrl}/{p.ImageUrl.TrimStart('/')}";
                            }
                            else
                            {
                                string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg",
                                                        "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg",
                                                        "ca_phe_trung.jpg", "com_tam.jpg" };
                                p.ImageUrl = localFoods[Math.Abs(p.Id) % localFoods.Length];
                            }
                            Points.Add(p);
                            _allPoints.Add(p);
                        }
                        Console.WriteLine($"[LOG] Loaded {Points.Count} stalls OK.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] LoadPointsAsync stalls failed: {ex.Message}");
            }

            // --- Load Top 5 Stalls from dedicated endpoint (real rating order) ---
            try
            {
                Console.WriteLine("[LOG] Fetching Top5 stalls from /api/Stalls/top5...");
                var top5Options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var top5Data = await _httpClient.GetFromJsonAsync<List<Stall>>("/api/Stalls/top5", top5Options);
                if (top5Data != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TopRatedPoints.Clear();
                        foreach (var p in top5Data)
                        {
                            if (!string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.StartsWith("http"))
                                p.ImageUrl = $"{AppConstants.BaseApiUrl}/{p.ImageUrl.TrimStart('/')}";
                            if (string.IsNullOrEmpty(p.ImageUrl))
                                p.ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=600";
                            TopRatedPoints.Add(p);
                        }
                        Console.WriteLine($"[LOG] Loaded {TopRatedPoints.Count} top-rated stalls OK.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] LoadPointsAsync top5 failed: {ex.Message}");
            }

            // --- Load Top Tours ---
            try
            {
                Console.WriteLine("[LOG] Fetching top tours from /api/Tours/top10...");
                var toursOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var toursData = await _httpClient.GetFromJsonAsync<List<Tour>>("/api/Tours/top10", toursOptions);
                if (toursData != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TopTours.Clear();
                        foreach (var t in toursData)
                        {
                            if (string.IsNullOrEmpty(t.ImageUrl))
                                t.ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600";
                            else if (!t.ImageUrl.StartsWith("http"))
                                t.ImageUrl = $"{AppConstants.BaseApiUrl}/{t.ImageUrl.TrimStart('/')}";
                            TopTours.Add(t);
                        }
                        Console.WriteLine($"[LOG] Loaded {TopTours.Count} tours OK.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] LoadPointsAsync tours failed: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // ═══════════════════════════════════════════
        // FILTERING & NAVIGATION
        // ═══════════════════════════════════════════

        private async void FilterAndNavigate(string keyword)
        {
            if (_allPoints == null || _allPoints.Count == 0) return;

            if (string.IsNullOrWhiteSpace(keyword) || keyword == "Tất cả" || keyword == "All")
            {
                await NavigateToPage(keyword, _allPoints);
                return;
            }

            var filtered = _allPoints
                .Where(p => p.Name != null && p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Fallback: show all if filter returns empty (prevents blank screen)
            if (filtered.Count == 0) filtered = _allPoints;

            await NavigateToPage(keyword, filtered);
        }

        private async Task NavigateToPage(string keyword, List<Stall> dataToPass)
        {
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.Windows[0].Page.Navigation
                        .PushAsync(new Views.FilterResultPage(keyword, dataToPass));
                });
            }
        }
    }
}