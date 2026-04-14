using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using HeriStep.Client.Services;
using HeriStep.Client.ViewModels;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class ShopDetailPage : ContentPage
    {
        private Stall _stall;
        private bool _isTtsPlaying = false;
        private Action? _langChangedHandler;
        private readonly AudioTranslationService _audioService;
        private readonly ShopDetailViewModel _viewModel;

        public ShopDetailPage(Stall stall, AudioTranslationService audioService)
        {
            Console.WriteLine("[CRITICAL_LOG] ShopDetailPage Constructor started...");
            try
            {
                InitializeComponent();
                _stall = stall;
                _audioService = audioService;
                _viewModel = new ShopDetailViewModel(new LocalDatabaseService(), _audioService, _stall);
                BindingContext = _viewModel;

                if (stall != null)
                {
                    shopName.Text = string.IsNullOrEmpty(stall.Name) 
                        ? "Signature Experience" 
                        : stall.Name;

                    if (!string.IsNullOrEmpty(stall.ImageUrl))
                    {
                        heroImage.Source = stall.ImageUrl;
                    }
                    else
                    {
                        string[] localFoods = { "pho_bo.jpg", "banh_mi.jpg", "oc_len.jpg", "bun_bo_hue.jpg", 
                                                "goi_cuon.jpg", "hu_tieu.jpg", "banh_xeo.jpg", "che_ba_mau.jpg", 
                                                "ca_phe_trung.jpg", "com_tam.jpg" };
                        int index = Math.Abs(stall.Id) % localFoods.Length;
                        heroImage.Source = localFoods[index];
                    }
                }

                // Subscribe language changes
                ApplyLocalization();
                _langChangedHandler = () => MainThread.BeginInvokeOnMainThread(ApplyLocalization);
                L.LanguageChanged += _langChangedHandler;
                _ = LoadMenuFromLocalDbAsync();
                Console.WriteLine("[CRITICAL_LOG] ShopDetailPage Constructor finished successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL_LOG] ShopDetailPage FATAL INSTANTIATION CRASH: {ex.Message}\n{ex.StackTrace}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel?.Cleanup();
            // Unsubscribe so we don't leak the static event reference
            if (_langChangedHandler != null)
            {
                L.LanguageChanged -= _langChangedHandler;
                _langChangedHandler = null;
            }
        }

        private void ApplyLocalization()
        {
            lblShopAddress.Text = L.Get("shop_address");
            lblMenuTitle.Text = L.Get("shop_menu_title");
            lblMenuDesc.Text = L.Get("shop_menu_desc");
            if (!_isTtsPlaying && _viewModel != null)
            {
                btnPlayIntro.Text = "🔊  " + L.Get("map_listen").Replace("🔊", string.Empty).Trim();
            }
        }

        private async Task LoadMenuFromLocalDbAsync()
        {
            try
            {
                if (_stall == null || _stall.Id <= 0)
                {
                    Console.WriteLine("[SHOP_DETAIL] Stall invalid, skip menu query.");
                    return;
                }

                await _viewModel.LoadMenuItemsAsync(_stall.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SHOP_DETAIL] LoadMenuFromLocalDbAsync failed: {ex.Message}");
            }
        }

        private void OnPlayIntroClicked(object sender, EventArgs e)
        {
            if (_isTtsPlaying) return;
            if (_stall == null) return;

            // Notice: The actual audio playing is now inside PlayAudioCommand in ViewModel.
            // This handler is just kept for UI visual indication logic, though it could be 
            // moved to ViewModel too. We just wrap visual feedback here.
            _isTtsPlaying = true;
            btnPlayIntro.IsEnabled = false;
            btnPlayIntro.Text = "⏳ " + L.Get("notification");
            lblTtsStatus.Text = "🔊 Using Voice Aura...";
            lblTtsStatus.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FF8C00");

            Task.Run(async () =>
            {
                await Task.Delay(1500); // simulate the loading wait, then reset text since ViewModel plays it.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblTtsStatus.Text = "✅ Success.";
                    lblTtsStatus.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
                    _isTtsPlaying = false;
                    btnPlayIntro.IsEnabled = true;
                    btnPlayIntro.Text = "🔊  " + L.Get("map_listen").Replace("🔊", string.Empty).Trim();
                });
            });
        }

        private string BuildFallback()
        {
            string lang = L.CurrentLanguage;
            string name = _stall?.Name ?? "sạp này";
            return lang switch
            {
                "en" => $"Welcome to {name}! Come enjoy the best street food in Vinh Khanh.",
                "ja" => $"{name}へようこそ！",
                "ko" => $"{name}에 오신 것을 환영합니다!",
                "fr" => $"Bienvenue à {name} !",
                "es" => $"¡Bienvenido a {name}!",
                _    => $"Chào mừng bạn đến {name}! Hãy thưởng thức ẩm thực Vĩnh Khánh nhé."
            };
        }
        private async void OnFeatureComingSoon(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("coming_soon"), L.Get("shop_feature_soon"), L.Get("ok"));
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}