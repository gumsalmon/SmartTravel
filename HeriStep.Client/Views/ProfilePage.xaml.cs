using System;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.Client.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly AudioTranslationService _audioService;
        private readonly LocalDatabaseService _localDb = new();
        private Action? _langChangedHandler;
        public ObservableCollection<ProfileVisitItem> RecentVisits { get; } = new();

        public ProfilePage(SubscriptionService subscriptionService, AudioTranslationService audioService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
            _audioService = audioService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ApplyLocalization();

            // Subscribe once to language changes
            if (_langChangedHandler == null)
            {
                _langChangedHandler = () => MainThread.BeginInvokeOnMainThread(ApplyLocalization);
                L.LanguageChanged += _langChangedHandler;
            }

            // Device ID
            lblDeviceId.Text = $"{L.Get("profile_device")} {_subscriptionService.GetDeviceId()}";

            // Subscription status
            var status = await _subscriptionService.CheckStatusAsync();
            if (status != null && status.Valid)
            {
                var hrs = status.RemainingHours ?? 0;
                var days = (int)Math.Floor(hrs / 24);
                var hours = (int)Math.Floor(hrs % 24);
                lblTimeRemaining.Text = string.Format(L.Get("profile_expiry_ok"), days, hours);
                lblTimeRemaining.TextColor = Colors.Gold;
            }
            else
            {
                lblTimeRemaining.Text = L.Get("profile_expiry_expired");
                lblTimeRemaining.TextColor = Colors.IndianRed;
            }

            await LoadVisitHistoryAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_langChangedHandler != null)
            {
                L.LanguageChanged -= _langChangedHandler;
                _langChangedHandler = null;
            }
        }

        private void ApplyLocalization()
        {
            lblProfileTitle.Text      = L.Get("profile_title");
            lblProfileDisplayName.Text= L.Get("profile_display_name");
            lblStatVisited.Text       = L.Get("profile_visited");
            lblStatSaved.Text         = L.Get("profile_saved_lbl");
            lblStatRating.Text        = L.Get("profile_rating");
            lblChangeLang.Text        = L.Get("profile_change_lang");
            lblChangeLangDesc.Text    = L.Get("profile_lang_desc");
            lblSaved.Text             = L.Get("profile_saved_lbl");
            lblSavedCount.Text        = $"{RecentVisits.Count} {L.Get("profile_saved_lbl").ToLower()}";
            lblSupport.Text           = L.Get("profile_support");
            lblSupportDesc.Text       = L.Get("profile_support_24");
            lblRecentHistory.Text     = L.Get("profile_history");
            lblViewAll.Text           = L.Get("profile_view_all");
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnSavedSpotsTapped(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("notification"), "Khu vực địa điểm đã lưu đang được cập nhật.", L.Get("ok"));
        }

        private async void OnSupportTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Email: support@heristep.local\nHotline: 1800-HERISTEP", L.Get("ok"));
        }

        private async void OnViewAllHistoryTapped(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("profile_history"), "Full history view coming soon.", L.Get("ok"));
        }

        private async Task LoadVisitHistoryAsync()
        {
            try
            {
                var summary = await _localDb.GetProfileVisitSummaryAsync();
                lblStatVisitedValue.Text = summary.TotalVisits.ToString();
                lblStatSavedValue.Text = summary.UniqueStalls.ToString();
                lblStatRatingValue.Text = summary.TotalVisits > 0 ? "★ 5.0" : "★ -";

                var topVisited = await _localDb.GetTopVisitedStallsAsync(5);
                RecentVisits.Clear();
                foreach (var item in topVisited)
                {
                    RecentVisits.Add(new ProfileVisitItem
                    {
                        StallId = item.StallId,
                        StallName = item.StallName,
                        VisitCount = item.VisitCount,
                        ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl)
                            ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600"
                            : item.ImageUrl,
                        VisitLabel = $"{L.Get("profile_visited")}: {item.VisitCount}"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PROFILE] LoadVisitHistory failed: {ex.Message}");
                RecentVisits.Clear();
                lblStatVisitedValue.Text = "0";
                lblStatSavedValue.Text = "0";
                lblStatRatingValue.Text = "★ -";
            }
        }

        private async void OnHistorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection.FirstOrDefault() is not ProfileVisitItem visitItem)
                {
                    return;
                }

                ((CollectionView)sender).SelectedItem = null;

                var localStalls = await _localDb.GetStallsAsync();
                var matched = localStalls.FirstOrDefault(s => s.Id == visitItem.StallId);
                var stall = new Stall
                {
                    Id = visitItem.StallId,
                    Name = visitItem.StallName,
                    ImageUrl = visitItem.ImageUrl,
                    Description = matched?.Description,
                    Latitude = matched?.Latitude ?? 0,
                    Longitude = matched?.Longitude ?? 0,
                    RadiusMeter = (int)(matched?.RadiusMeter ?? 20),
                    IsOpen = matched?.IsOpen ?? true,
                    TtsScript = matched?.TtsScript
                };

                await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROFILE] OnHistorySelectionChanged failed: {ex.Message}");
            }
        }

        private async void OnChangeLanguageTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LanguagePage(isChangeMode: true));
        }

        public class ProfileVisitItem
        {
            public int StallId { get; set; }
            public string StallName { get; set; } = string.Empty;
            public int VisitCount { get; set; }
            public string VisitLabel { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}
