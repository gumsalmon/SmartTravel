using System;
using System.Linq;
using HeriStep.Client.Views;
using HeriStep.Client.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        // Savory Ember design tokens
        private static readonly Color CardActive   = Color.FromArgb("#3D1A00");
        private static readonly Color CardInactive = Color.FromArgb("#2A2A2A");
        private static readonly Color StrokeActive = Color.FromArgb("#FF8C00");
        private static readonly Color StrokeMuted  = Color.FromArgb("#3A3A3A");

        private string _selectedGender = "female";
        private string _selectedLangMode = "vi";
        private bool _isTtsPlaying = false;

        public MainPage()
        {
            InitializeComponent();
            voiceTextInput.TextChanged += OnVoiceTextChanged;
            pickerLanguage.SelectedIndex = 0;
            SetDefaultPreviewText();
            ApplyLocalization();
            L.LanguageChanged += () => MainThread.BeginInvokeOnMainThread(() => 
            {
                ApplyLocalization();
                SetDefaultPreviewText();
            });
        }

        private void ApplyLocalization()
        {
            lblMainHeader.Text = L.Get("main_header");
            lblHeroTag.Text = L.Get("main_hero_tag");
            lblHeroTitle.Text = L.Get("main_hero_title");
            
            lblQuickMap.Text = L.Get("main_btn_map");
            lblQuickMapDesc.Text = L.Get("main_map_desc");
            lblQuickTop.Text = L.Get("main_top_shops");
            lblQuickTopDesc.Text = L.Get("main_top_shops_desc");
            
            lblTopSection.Text = L.Get("main_section_top").Replace("\n", " ");
            lblTopSectionDesc.Text = L.Get("main_section_top_desc").Replace("\n", " ");
            lblViewAll.Text = L.Get("main_view_all").Replace("\n", " ");
            
            lblShop1Tags.Text = L.Get("main_shop1_tags");
            lblShop2Tags.Text = L.Get("main_shop2_tags");
            
            lblVoiceTag.Text = L.Get("main_voice_tag");
            lblVoiceTitle.Text = L.Get("main_voice_title");
            lblVoiceDesc.Text = L.Get("main_voice_desc");
            lblVoiceProfileRow.Text = L.Get("main_voice_profile");
            lblVoiceTextRow.Text = L.Get("main_voice_text");
            lblVoiceSpeedRow.Text = L.Get("main_voice_speed");
            btnTestVoice.Text = "▶   " + L.Get("main_voice_btn");
            voiceTextInput.Placeholder = L.Get("main_voice_placeholder");
            lblVoiceStatus.Text = "💡 " + L.Get("main_voice_status_default");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyLocalization();
            // Pre-warm TTS
            _ = Task.Run(async () =>
            {
                try { await TextToSpeech.Default.GetLocalesAsync(); }
                catch (Exception ex) { Console.WriteLine($"[CRITICAL_LOG] Pre-warm TTS Failed: {ex.Message}"); }
            });
        }

        private void SetDefaultPreviewText()
        {
            var lang = Preferences.Default.Get("user_language", "vi");
            voiceTextInput.Text = lang switch
            {
                "en" => "Welcome to Vinh Khanh Food Street! Come enjoy the most delicious street food in Saigon.",
                "ja" => "ヴィンカーン通りへようこそ！サイゴンで最も美味しい屋台料理をお楽しみください。",
                "ko" => "빈칸 음식 거리에 오신 것을 환영합니다! 맛있는 길거리 음식을 즐겨보세요.",
                "zh" => "欢迎来到永庆美食街！品味西贡最美味的街头美食。",
                "fr" => "Bienvenue dans la rue Vinh Khanh ! Dégustez la meilleure cuisine de rue de Saïgon.",
                _    => "Chào mừng bạn đến Phố Vĩnh Khánh! Hãy thưởng thức ẩm thực đường phố ngon nhất Sài Gòn."
            };
        }

        // ════════════════════════════════════════════
        // NAVIGATION
        // ════════════════════════════════════════════

        private async void OnMenuClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("//ProfilePage");

        private async void OnAvatarTapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("//ProfilePage");

        private async void BtnMap_Clicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("//MapPage");

        private async void BtnShop_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine("[CRITICAL_LOG] BtnShop_Clicked invoked.");
            try
            {
                var stall = new HeriStep.Shared.Models.Stall
                {
                    Id = 1, Name = "Ốc Oanh Vĩnh Khánh",
                    ImageUrl = "https://images.unsplash.com/photo-1548690312-e3b507d8c110?w=600"
                };
                Console.WriteLine("[CRITICAL_LOG] Attempting to PushAsync(new ShopDetailPage(stall))...");
                await Navigation.PushAsync(new ShopDetailPage(stall));
                Console.WriteLine("[CRITICAL_LOG] PushAsync succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL_LOG] FATAL CRASH IN NAV TO ShopDetailPage:\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private async void BtnShop2_Clicked(object sender, EventArgs e)
        {
            var stall = new HeriStep.Shared.Models.Stall
            {
                Id = 2, Name = "Bà Kẹp Vĩnh Khánh",
                ImageUrl = "https://images.unsplash.com/photo-1574484284002-952d92456975?w=600"
            };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        // ════════════════════════════════════════════
        // VOICE STUDIO UI EVENTS
        // ════════════════════════════════════════════

        private void OnGenderTapped(object sender, TappedEventArgs e)
        {
            _selectedGender = e.Parameter as string ?? "female";
            UpdateVoiceGenderUI();
        }

        private void UpdateVoiceGenderUI()
        {
            btnVoiceFemale.BackgroundColor = _selectedGender == "female" ? CardActive : CardInactive;
            btnVoiceFemale.Stroke = _selectedGender == "female" ? StrokeActive : StrokeMuted;
            btnVoiceFemale.StrokeThickness = _selectedGender == "female" ? 2 : 1;

            btnVoiceMale.BackgroundColor = _selectedGender == "male" ? CardActive : CardInactive;
            btnVoiceMale.Stroke = _selectedGender == "male" ? StrokeActive : StrokeMuted;
            btnVoiceMale.StrokeThickness = _selectedGender == "male" ? 2 : 1;

            UpdateStatusText();
        }

        private void OnLanguagePickerChanged(object sender, EventArgs e)
        {
            int index = pickerLanguage.SelectedIndex;
            _selectedLangMode = index switch
            {
                0 => "vi", 1 => "en", 2 => "ko", 3 => "ja", 4 => "fr", 5 => "es", _ => "vi"
            };
            SetDefaultPreviewText();
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            lblVoiceStatus.Text = $"✅ Đã chọn Giọng {(_selectedGender == "female" ? "Nữ" : "Nam")} - {_selectedLangMode.ToUpper()}";
            lblVoiceStatus.TextColor = Color.FromArgb("#FF8C00");
        }
        
        // ════════════════════════════════════════════
        // HERO SEARCH LOGIC
        // ════════════════════════════════════════════
        
        private async void OnHeroSearchCompleted(object sender, EventArgs e)
        {
            var text = heroSearchEntry.Text?.Trim().ToLower();
            if (string.IsNullOrEmpty(text)) return;
            
            // For now, mock route to "Ốc Oanh" on search
            var stall = new HeriStep.Shared.Models.Stall
            {
                Id = 1, Name = "Kết quả Search: " + text,
                ImageUrl = "https://images.unsplash.com/photo-1548690312-e3b507d8c110?w=600"
            };
            
            heroSearchEntry.Text = "";
            await Navigation.PushAsync(new ShopDetailPage(stall));
        }

        // ════════════════════════════════════════════
        // TEXT / SPEED HELPERS
        // ════════════════════════════════════════════

        private void OnVoiceTextChanged(object? sender, TextChangedEventArgs e)
        {
            int count = e.NewTextValue?.Length ?? 0;
            lblCharCount.Text = $"{count} / 300";
            lblCharCount.TextColor = count > 250
                ? Color.FromArgb("#EF4444")
                : Color.FromArgb("#6B5B4E");
        }

        private void OnSpeedChanged(object sender, ValueChangedEventArgs e)
            => lblSpeedVal.Text = $"{e.NewValue:0.0}×";

        // ════════════════════════════════════════════
        // TEST VOICE — immediate TTS preview
        // ════════════════════════════════════════════

        private async void OnTestVoiceClicked(object sender, EventArgs e)
        {
            Console.WriteLine("[CRITICAL_LOG] OnTestVoiceClicked invoked.");
            if (_isTtsPlaying) return;

            var text = voiceTextInput.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                Console.WriteLine("[CRITICAL_LOG] TTS Input was empty!");
                lblVoiceStatus.Text = "⚠️ Please enter some text first.";
                lblVoiceStatus.TextColor = Color.FromArgb("#FFBF00");
                return;
            }

            _isTtsPlaying = true;
            btnTestVoice.Text = "⏳  Playing...";
            btnTestVoice.IsEnabled = false;
            lblVoiceStatus.Text = "🔊 Speaking...";
            lblVoiceStatus.TextColor = Color.FromArgb("#D35400");

            try
            {
                Console.WriteLine($"[CRITICAL_LOG] Fetching Locales. Selected profile: {_selectedGender}_{_selectedLangMode}");
                var locales  = await TextToSpeech.Default.GetLocalesAsync();
                
                Console.WriteLine($"[CRITICAL_LOG] Machine returned {locales?.Count() ?? 0} locales. Looking for {_selectedLangMode}.");
                var localeList = locales?.Where(l => l.Language.StartsWith(_selectedLangMode, StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Fallback to exactly null if language not found (system will default to basic TTS voice)
                var bestMatch = localeList?.FirstOrDefault();
                
                // Some OS support filtering by name for gender, but it varies wildly.
                
                var options = new SpeechOptions
                {
                    Pitch  = _selectedGender == "female" ? 1.2f : 0.8f,
                    Volume = 1.0f,
                    Rate   = (float)voiceSpeedSlider.Value
                };
                
                if (bestMatch != null)
                {
                    options.Locale = bestMatch;
                }

                Console.WriteLine("[CRITICAL_LOG] Awaiting TextToSpeech.SpeakAsync...");
                await TextToSpeech.Default.SpeakAsync(text, options);
                Console.WriteLine("[CRITICAL_LOG] TTS SpeakAsync completed successfully with audio.");

                lblVoiceStatus.Text = "✅ " + L.Get("aura_saved_ok");
                lblVoiceStatus.TextColor = Color.FromArgb("#D35400");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL_LOG] TTS FAILED TO PLAY AUDIO: {ex.Message}");
                lblVoiceStatus.Text = $"❌ Không thể phát (Emulator thiếu gói ngôn ngữ)";
                lblVoiceStatus.TextColor = Color.FromArgb("#EF4444");
                await DisplayAlert("Lỗi Emulator Audio", "Lỗi Text-to-Speech do máy ảo không có sẵn gói âm thanh này. Vui lòng vào Cài đặt máy ảo -> Accessibility -> Text-to-Speech output để tải xuống, hoặc test trên điện thoại thật.", "Đã hiểu");
            }
            finally
            {
                _isTtsPlaying = false;
                btnTestVoice.Text = "▶   " + L.Get("main_voice_btn");
                btnTestVoice.IsEnabled = true;
                Console.WriteLine("[CRITICAL_LOG] TTS Logic exited.");
            }
        }
    }
}