using System;
using System.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HeriStep.Client.Views
{
    public partial class VoiceAuraPage : ContentPage
    {
        private string _selectedLang = "vi";
        private string _selectedGender = "Female";

        public VoiceAuraPage()
        {
            InitializeComponent();
            
            speedSlider.ValueChanged += (s, e) => lblSpeedValue.Text = $"{e.NewValue:0.0}x";
            radiusSlider.ValueChanged += (s, e) => lblRadiusValue.Text = $"{(int)e.NewValue}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSettings();

            // 💡 FORCED INIT: Silent speak to force Android TTS engine to wake up early.
            _ = Task.Run(async () => {
                try { 
                    await TextToSpeech.Default.GetLocalesAsync(); 
                    await TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });
                } catch { }
            });
        }

        private void LoadSettings()
        {
            _selectedLang = Preferences.Default.Get("user_language", "vi");
            _selectedGender = Preferences.Default.Get("voice_gender", "Female");
            var speed = Preferences.Default.Get("voice_speed", 1.2);
            var radius = Preferences.Default.Get("voice_radius", 50.0);

            speedSlider.Value = speed;
            radiusSlider.Value = radius;
            
            UpdateLanguageUI();
            UpdateGenderUI();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private void OnLanguageTapped(object sender, EventArgs e)
        {
            if (e is TappedEventArgs tapped && tapped.Parameter is string lang)
            {
                _selectedLang = lang;
                UpdateLanguageUI();
            }
        }

        private void OnGenderTapped(object sender, EventArgs e)
        {
            if (e is TappedEventArgs tapped && tapped.Parameter is string gender)
            {
                _selectedGender = gender;
                UpdateGenderUI();
            }
        }

        private void UpdateLanguageUI()
        {
            ResetFrames(frameVN, frameEN, frameCN, frameJP, frameKR, frameFR, frameES, frameDE, frameTH);
            var activeFrame = _selectedLang switch
            {
                "vi" => frameVN,
                "en" => frameEN,
                "zh" => frameCN,
                "ja" => frameJP,
                "ko" => frameKR,
                "fr" => frameFR,
                "es" => frameES,
                "de" => frameDE,
                "th" => frameTH,
                _ => frameVN
            };
            if (activeFrame != null)
            {
                activeFrame.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#00695C");
                var hsl = activeFrame.Content as HorizontalStackLayout;
                if (hsl != null && hsl.Children.Count > 1 && hsl.Children[1] is Label lbl)
                {
                    lbl.TextColor = Colors.White;
                }
            }
        }

        private void UpdateGenderUI()
        {
            ResetFrames(frameMale, frameFemale);
            var activeFrame = _selectedGender == "Male" ? frameMale : frameFemale;
            if (activeFrame != null)
            {
                activeFrame.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0F2F1");
                activeFrame.BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#00695C");
            }
        }

        private void ResetFrames(params Frame[] frames)
        {
            foreach (var f in frames)
            {
                if (f == null) continue;
                f.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F0FDFC");
                f.BorderColor = Colors.Transparent;
                if (f.Content is HorizontalStackLayout hsl && hsl.Children.Count > 1 && hsl.Children[1] is Label lbl)
                    lbl.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#004D40");
                if (f.Content is VerticalStackLayout vsl && vsl.Children.Count > 1 && vsl.Children[1] is Label vlbl)
                    vlbl.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#004D40");
            }
        }

        private async void OnPreviewClicked(object sender, EventArgs e)
        {
            try
            {
                var speed = (float)speedSlider.Value;
                var text = _selectedLang switch
                {
                    "en" => "Welcome to Vĩnh Khánh Food Street!",
                    "zh" => "欢迎来到永庆美食街！",
                    "ja" => "ビンカンフードストリートへようこそ！",
                    "ko" => "빈칸 음식 거리 에 오신 것을 환영합니다!",
                    "fr" => "Bienvenue dans la rue de la nourriture de Vĩnh Khánh !",
                    "es" => "¡Bienvenido a la calle de la comida de Vĩnh Khánh!",
                    "de" => "Willkommen in der Vĩnh Khánh Food Street!",
                    "th" => "ยินดีต้อนรับสู่ถนนสายอาหารหวิงคานห์!",
                    _ => "Chào mừng bạn đến với Phố ẩm thực Vĩnh Khánh!"
                };

                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = locales?.FirstOrDefault(l => l.Language.StartsWith(_selectedLang, StringComparison.OrdinalIgnoreCase));

                await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions 
                { 
                    Pitch = 1.0f, 
                    Volume = 1.0f,
                    Locale = locale
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể phát âm thanh: " + ex.Message, "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            Preferences.Default.Set("user_language", _selectedLang);
            Preferences.Default.Set("voice_gender", _selectedGender);
            Preferences.Default.Set("voice_speed", speedSlider.Value);
            Preferences.Default.Set("voice_radius", radiusSlider.Value);

            await DisplayAlert("Thành công", "Thiết lập đã được lưu lại.", "OK");
        }
    }
}
