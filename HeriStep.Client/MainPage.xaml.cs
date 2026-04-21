using System;
using System.Linq;
using System.Collections.Generic;
using HeriStep.Client.Views;
using HeriStep.Client.Services;
using HeriStep.Client.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        private readonly AudioTranslationService _audioService;
        private readonly HomeViewModel _homeViewModel;
        private Action? _languageChangedHandler;

        public MainPage(AudioTranslationService audioService, HomeViewModel homeViewModel)
        {
            InitializeComponent();
            _audioService = audioService;
            _homeViewModel = homeViewModel;
            BindingContext = _homeViewModel;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            lblMainHeader.Text = L.Get("main_header");
            lblHeroTag.Text = L.Get("main_hero_tag");
            lblHeroTitle.Text = L.Get("main_hero_title");
            lblTopSection.Text = L.Get("main_section_top").Replace("\n", " ");
            lblViewAll.Text = L.Get("main_view_all").Replace("\n", " ");

            lblHeroDesc.Text = L.Get("main_hero_desc") == "main_hero_desc" ? "Khám phá khu ẩm thực sầm uất bậc nhất Sài Gòn" : L.Get("main_hero_desc");
            lblCatTitle.Text = L.Get("main_cat_title") == "main_cat_title" ? "Hoạt Động Nổi Bật" : L.Get("main_cat_title");
            lblCatHot.Text = L.Get("main_cat_hot") == "main_cat_hot" ? "Đang Hot" : L.Get("main_cat_hot");
            lblCatSeafood.Text = L.Get("main_cat_seafood") == "main_cat_seafood" ? "Ốc & Hải Sản" : L.Get("main_cat_seafood");
            lblCatDrinks.Text = L.Get("main_cat_drinks") == "main_cat_drinks" ? "Đồ Uống" : L.Get("main_cat_drinks");
            lblCatSnacks.Text = L.Get("main_cat_snacks") == "main_cat_snacks" ? "Ăn Vặt" : L.Get("main_cat_snacks");
            lblPromoTitle.Text = L.Get("main_promo_title") == "main_promo_title" ? "Khám Phá Tự Do 🎧" : L.Get("main_promo_title");
            lblPromoDesc.Text = L.Get("main_promo_desc") == "main_promo_desc" ? "Đồng bộ GPS — tự phát loa khi trải nghiệm" : L.Get("main_promo_desc");
            lblPromoBtn.Text = L.Get("main_promo_btn") == "main_promo_btn" ? "Thử ngay" : L.Get("main_promo_btn");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_languageChangedHandler != null)
            {
                L.LanguageChanged -= _languageChangedHandler;
                _languageChangedHandler = null;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_languageChangedHandler == null)
            {
                _languageChangedHandler = () => MainThread.BeginInvokeOnMainThread(ApplyLocalization);
                L.LanguageChanged += _languageChangedHandler;
            }
            ApplyLocalization();
            _ = _homeViewModel.LoadPointsAsync();
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

        private async void Promo_Clicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("//MapPage");

        private async void OnTopShopSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is HeriStep.Shared.Models.Stall stall)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
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
            if (sender is SearchBar entry)
            {
                string keyword = entry.Text;
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var result = _homeViewModel.GetSearchSuggestions(keyword, 20);
                    await Shell.Current.Navigation.PushAsync(new FilterResultPage(keyword, result, _audioService));
                }
            }
        }

        private void OnHeroSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = e.NewTextValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                heroSuggestionPanel.IsVisible = false;
                heroSuggestionList.ItemsSource = null;
                return;
            }

            var suggestions = _homeViewModel.GetSearchSuggestions(keyword, 8);
            heroSuggestionList.ItemsSource = suggestions;
            heroSuggestionPanel.IsVisible = suggestions.Count > 0;
        }

        private async void OnHeroSuggestionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not HeriStep.Shared.Models.Stall selectedStall)
            {
                return;
            }

            heroSuggestionPanel.IsVisible = false;
            heroSearchEntry.Text = string.Empty;
            heroSuggestionList.SelectedItem = null;
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(selectedStall, _audioService));
        }
    }
}