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
        private readonly LocalDatabaseService _localDb;
        private readonly AudioTranslationService _audioService;

        public HomeViewModel(HttpClient httpClient, HeriStep.Client.Services.Location.ILocationService locationService, LocalDatabaseService localDb, AudioTranslationService audioService)
        {
            _httpClient = httpClient;
            _locationService = locationService;
            _localDb = localDb;
            _audioService = audioService;
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
                string message = !string.IsNullOrEmpty(stall.Description) 
                    ? stall.Description 
                    : $"Chào mừng bạn đến với {stall.Name}!";

                // Automation: Auto-translate and Speak in background
                await _audioService.SpeakAsync(message);
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
        /// Loads all stalls and tours. Áp dụng kiến trúc Offline-first:
        /// - Top5 Quán và Top10 Tour được load từ SQLite NGay Lập Tức (0 giây, không cần mạng)
        /// - API được gọi background sau đó để cập nhật dữ liệu mới
        /// </summary>
        public async Task LoadPointsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // =====================================================================
                // Úu Tiên 1: OFFLINE-FIRST — Load Top5 + Top10 từ SQLite NGay Lập Tức
                // ViewModel trực tiếp thực thi truy vấn SELECT từ CSDL SQLite nhúng sẵn
                // đẩy thẳng lên UI Thread, không chờ API.
                // =====================================================================
                await LoadTop5FromSQLiteAsync();
                await LoadTop10ToursFromSQLiteAsync();

                // =====================================================================
                // Úu Tiên 2: API HTTP — cập nhật dữ liệu đầy đủ và lưu vào SQLite cache
                // =====================================================================
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
                    
                    // SAVE TO SQLITE (OFFLINE CACHE)
                    try 
                    {
                        var cacheStalls = data.Select(p => new HeriStep.Client.Models.LocalModels.LocalStall {
                            Id = p.Id,
                            Name = p.Name ?? "Chưa có tên",
                            Description = p.Description ?? p.Name ?? "Chào mừng bạn, hiện tại ứng dụng đang ở chế độ ngoại tuyến.",
                            ImageUrl = p.ImageUrl ?? "",
                            Latitude = p.Latitude,
                            Longitude = p.Longitude,
                            IsOpen = p.IsOpen,
                            HasOwner = p.OwnerId.HasValue,
                            RadiusMeter = p.RadiusMeter,
                            Rating = 4.5 // Mặc định cho đến khi có dữ liệu thực tế từ API
                        });
                        await _localDb.SaveStallsAsync(cacheStalls);
                    } 
                    catch (Exception dbEx) 
                    {
                        Console.WriteLine($"[OFFLINE_DB] Failed to cache stalls: {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE_DB] Fetching API stalls failed: {ex.Message}");
                // OFFLINE FALLBACK
                try 
                {
                    var offlineStalls = await _localDb.GetStallsAsync();
                    if (offlineStalls != null && offlineStalls.Count > 0)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            Points.Clear(); _allPoints.Clear();
                            foreach (var ls in offlineStalls)
                            {
                                var p = new Stall {
                                    Id = ls.Id,
                                    Name = ls.Name,
                                    Description = ls.Description,
                                    ImageUrl = ls.ImageUrl,
                                    Latitude = ls.Latitude,
                                    Longitude = ls.Longitude,
                                    IsOpen = ls.IsOpen,
                                    OwnerId = ls.HasOwner ? 1 : null,
                                    RadiusMeter = (int)ls.RadiusMeter
                                };
                                Points.Add(p);
                                _allPoints.Add(p);
                            }
                            Console.WriteLine($"[OFFLINE_DB] Loaded {Points.Count} stalls from SQLite.");
                            await CommunityToolkit.Maui.Alerts.Toast.Make("Mất kết nối mạng. Đang hiển thị danh sách quán lưu tạm.").Show();
                        });
                    }
                }
                catch (Exception readEx)
                {
                    Console.WriteLine($"[OFFLINE_DB] Cannot read stalls from SQLite: {readEx.Message}");
                }
            }

            // --- Background Refresh Top 5 từ API (sau khi đã hiển thị SQLite rồi) ---
            _ = Task.Run(async () => await RefreshTop5FromApiAsync());

            // --- Background Refresh Top 10 Tours từ API ---
            _ = Task.Run(async () => await RefreshTop10ToursFromApiAsync());

            IsBusy = false;
        }

        // ===================================================================
        // OFFLINE-FIRST: Đọc truy vấn từ SQLite ngay lập tức (không gọi API)
        // ===================================================================

        /// <summary>
        /// SELECT TOP 5 FROM StallCache ORDER BY Rating DESC
        /// Hiển thị ngay 0 giây, không cần internet.
        /// </summary>
        private async Task LoadTop5FromSQLiteAsync()
        {
            try
            {
                var top5Local = await _localDb.GetTop5StallsAsync();
                if (top5Local != null && top5Local.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TopRatedPoints.Clear();
                        foreach (var ls in top5Local)
                        {
                            TopRatedPoints.Add(new Stall
                            {
                                Id = ls.Id,
                                Name = ls.Name,
                                Description = ls.Description,
                                ImageUrl = string.IsNullOrEmpty(ls.ImageUrl)
                                    ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=600"
                                    : ls.ImageUrl,
                                Latitude = ls.Latitude,
                                Longitude = ls.Longitude,
                                IsOpen = ls.IsOpen
                            });
                        }
                        Console.WriteLine($"[OFFLINE-FIRST] Top5 từ SQLite: {top5Local.Count} quán — 0 giây.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE_DB] SQLite Top5 failed: {ex.Message}");
            }
        }

        /// <summary>
        /// SELECT * FROM TourCache WHERE IsActive=1 LIMIT 10
        /// Hiển thị ngay 0 giây, không cần internet.
        /// </summary>
        private async Task LoadTop10ToursFromSQLiteAsync()
        {
            try
            {
                var top10Local = await _localDb.GetTop10ToursAsync();
                if (top10Local != null && top10Local.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TopTours.Clear();
                        foreach (var lt in top10Local)
                        {
                            TopTours.Add(new Tour
                            {
                                Id = lt.Id,
                                TourName = lt.TourName,
                                ImageUrl = string.IsNullOrEmpty(lt.ImageUrl)
                                    ? "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600"
                                    : lt.ImageUrl,
                                StallCount = lt.StallCount,
                                Visits = lt.Visits
                            });
                        }
                        Console.WriteLine($"[OFFLINE-FIRST] Top10 Tours từ SQLite: {top10Local.Count} lộ trình — 0 giây.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE_DB] SQLite Top10Tours failed: {ex.Message}");
            }
        }

        // ===================================================================
        // BACKGROUND SYNC: Refresh từ API và cập nhật UI + SQLite cache
        // ===================================================================

        private async Task RefreshTop5FromApiAsync()
        {
            try
            {
                Console.WriteLine("[LOG] Background: Fetching Top5 stalls from /api/Stalls/top5...");
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
                        Console.WriteLine($"[LOG] Background: Top5 API loaded {TopRatedPoints.Count} stalls.");
                    });

                    // Cập nhật SQLite cache với dữ liệu mới nhất từ API (kèm Rating nếu có)
                    try
                    {
                        var cacheStalls = top5Data.Select((p, i) => new HeriStep.Client.Models.LocalModels.LocalStall
                        {
                            Id = p.Id,
                            Name = p.Name ?? "",
                            Description = p.Description ?? "",
                            ImageUrl = p.ImageUrl ?? "",
                            Latitude = p.Latitude,
                            Longitude = p.Longitude,
                            IsOpen = p.IsOpen,
                            HasOwner = p.OwnerId.HasValue,
                            RadiusMeter = p.RadiusMeter,
                            Rating = 5.0 - (i * 0.1) // Top5 sắp xếp từ cao xuống thấp
                        });
                        await _localDb.SaveStallsAsync(cacheStalls);
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[OFFLINE_DB] Failed to cache top5 stalls: {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE_DB] Background Top5 API failed (offline?): {ex.Message}");
                // Giữ nguyên dữ liệu SQLite đang hiển thị, không làm gì thêm
            }
        }

        private async Task RefreshTop10ToursFromApiAsync()
        {
            try
            {
                Console.WriteLine("[LOG] Background: Fetching Top10 tours from /api/Tours/top10...");
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
                        Console.WriteLine($"[LOG] Background: Top10 Tours API loaded {TopTours.Count} tours.");
                    });
                    
                    // SAVE TO SQLITE
                    try
                    {
                        var cacheTours = toursData.Select(t => new HeriStep.Client.Models.LocalModels.LocalTour {
                            Id = t.Id,
                            TourName = t.TourName,
                            Description = t.Description ?? "",
                            ImageUrl = t.ImageUrl ?? "",
                            StallCount = t.StallCount,
                            Visits = t.Visits,
                            IsActive = true
                        });
                        await _localDb.SaveToursAsync(cacheTours);
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[OFFLINE_DB] Failed to cache tours: {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OFFLINE_DB] Background Top10 Tours API failed (offline?): {ex.Message}");
                // Giữ nguyên dữ liệu SQLite đang hiển thị, không làm gì thêm
            }
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

            string searchKw = keyword.Trim();
            var compareInfo = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
            var filtered = _allPoints
                .Where(p => p.Name != null && compareInfo.IndexOf(p.Name, searchKw, System.Globalization.CompareOptions.IgnoreNonSpace | System.Globalization.CompareOptions.IgnoreCase) >= 0)
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
                        .PushAsync(new Views.FilterResultPage(keyword, dataToPass, _audioService));
                });
            }
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }
    }
}