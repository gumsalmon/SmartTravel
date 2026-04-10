using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using Microsoft.Maui.Controls;

namespace HeriStep.Client.Views
{
    public partial class ShopDetailPage : ContentPage
    {
        private Stall _stall;
        private bool _isTtsPlaying = false;
        private Action? _langChangedHandler;

        public ShopDetailPage(Stall stall)
        {
            Console.WriteLine("[CRITICAL_LOG] ShopDetailPage Constructor started...");
            try
            {
                InitializeComponent();
                _stall = stall;

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
            
            lblDish1Desc.Text = L.Get("shop_dish1_desc");
            lblDish2Desc.Text = L.Get("shop_dish2_desc");
            lblDish3Desc.Text = L.Get("shop_dish3_desc");
            
            lblDish1Tag1.Text = L.Get("shop_tag_musttry");
            lblDish1Tag2.Text = L.Get("shop_tag_spicy");
            
            btnOrder1.Text = "🛒  " + L.Get("shop_add_order");
        }

        private async void OnPlayIntroClicked(object sender, EventArgs e)
        {
            if (_isTtsPlaying) return;
            if (_stall == null || _stall.Id == 0)
            {
                lblTtsStatus.Text = "⚠️ Chưa có thông tin sạp.";
                return;
            }

            _isTtsPlaying = true;
            btnPlayIntro.IsEnabled = false;
            btnPlayIntro.Text = "⏳  Đang tải...";
            lblTtsStatus.Text = "🔊 Đang kết nối...";
            lblTtsStatus.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FF8C00");

            try
            {
                string lang = L.CurrentLanguage;
                string textToSpeak;

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                var url = $"{AppConstants.BaseApiUrl}/api/Stalls/{_stall.Id}/tts/{lang}";
                Console.WriteLine($"[LOG] Fetching TTS from: {url}");

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<TtsPayload>();
                    textToSpeak = (!string.IsNullOrWhiteSpace(data?.Text))
                        ? data!.Text
                        : BuildFallback();
                    Console.WriteLine($"[LOG] TTS text fetched OK ({textToSpeak.Length} chars).");
                }
                else
                {
                    Console.WriteLine($"[ERROR] TTS API returned {response.StatusCode}. Using fallback.");
                    textToSpeak = BuildFallback();
                }

                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = locales?.FirstOrDefault(l => l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase));

                lblTtsStatus.Text = "🔊 Đang phát...";
                await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch  = 1.0f,
                    Locale = locale
                });

                lblTtsStatus.Text = "✅ Phát xong.";
                lblTtsStatus.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#22C55E");
                Console.WriteLine("[LOG] TTS playback complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TTS OnPlayIntroClicked failed: {ex.Message}");
                lblTtsStatus.Text = $"❌ Lỗi: {ex.Message}";
                lblTtsStatus.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#EF4444");
            }
            finally
            {
                _isTtsPlaying = false;
                btnPlayIntro.IsEnabled = true;
                btnPlayIntro.Text = "🔊  Nghe Giới Thiệu";
            }
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

        private class TtsPayload { public string Text { get; set; } = ""; }

        private async void OnOrderClicked(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("notification"), L.Get("shop_order_alert"), L.Get("ok"));
        }

        private async void OnFeatureComingSoon(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("coming_soon"), L.Get("shop_feature_soon"), L.Get("ok"));
        }
    }
}