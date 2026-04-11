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
        private readonly AudioTranslationService _audioService;

        public MainPage(AudioTranslationService audioService)
        {
            InitializeComponent();
            _audioService = audioService;
            ApplyLocalization();
            L.LanguageChanged += () => MainThread.BeginInvokeOnMainThread(() => 
            {
                ApplyLocalization();
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
                Console.WriteLine("[CRITICAL_LOG] Attempting to PushAsync(new ShopDetailPage(stall, _audioService))...");
                await Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
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
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
        }


        private async void OnHeroSearchCompleted(object sender, EventArgs e)
        {
            if (sender is Entry entry)
            {
                string keyword = entry.Text;
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    // Redirect to filter result page (search logic)
                    await Shell.Current.Navigation.PushAsync(new FilterResultPage(keyword, new List<HeriStep.Shared.Models.Stall>(), _audioService));
                }
            }
        }
    }
}