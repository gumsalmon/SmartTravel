using System;
using Microsoft.Maui.Controls;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace HeriStep.Client.Views
{
    public partial class LanguageSelectionPage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly AudioTranslationService _audioService;
        private readonly LanguageCatalogService _languageCatalog = new();
        public ObservableCollection<LanguageSelectionItem> Languages { get; } = new();

        private static readonly Dictionary<string, string> FlagMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = "🇻🇳",
            ["en"] = "🇬🇧",
            ["ja"] = "🇯🇵",
            ["ko"] = "🇰🇷",
            ["zh"] = "🇨🇳",
            ["fr"] = "🇫🇷",
            ["es"] = "🇪🇸",
            ["ru"] = "🇷🇺",
            ["th"] = "🇹🇭",
            ["de"] = "🇩🇪"
        };

        public LanguageSelectionPage(SubscriptionService subscriptionService, AudioTranslationService audioService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
            _audioService = audioService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Languages.Count == 0)
            {
                var languages = await _languageCatalog.GetLanguagesAsync();
                Languages.Clear();
                foreach (var language in languages)
                {
                    Languages.Add(new LanguageSelectionItem
                    {
                        LangCode = language.LangCode,
                        LangName = language.LangName,
                        Flag = FlagMap.TryGetValue(language.LangCode, out var flag) ? flag : "🌐"
                    });
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }
        public bool IsNotBusy => !IsBusy;

        private async void OnLanguageSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is LanguageSelectionItem language && !string.IsNullOrWhiteSpace(language.LangCode))
            {
                if (IsBusy) return;
                IsBusy = true;

                try
                {
                    // 1. Chạy logic đổi ngôn ngữ trên Background Thread để không block UI
                    string targetLang = language.LangCode;
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"[LANG_SWITCH] Starting switch to {targetLang} on Background Thread...");
                        L.SetLanguage(targetLang);
                        Console.WriteLine($"[LANG_SWITCH] SetLanguage completed successfully.");
                    });

                    // 2. Quay lại UI Thread để chuyển trang
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Console.WriteLine($"[LANG_SWITCH] Navigating to LoadingPage...");
                        Application.Current.MainPage = new LoadingPage(_subscriptionService, _audioService);
                    });
                }
                catch (Exception ex)
                {
                    // 3. Try-Catch để tránh văng app khi có lỗi bất ngờ
                    Console.WriteLine($"[LANG_SWITCH] FATAL ERROR during language change: {ex.Message}");
                    await DisplayAlert("Lỗi", "Không thể chuyển đổi ngôn ngữ. Vui lòng thử lại sau.", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public class LanguageSelectionItem
        {
            public string LangCode { get; set; } = string.Empty;
            public string LangName { get; set; } = string.Empty;
            public string Flag { get; set; } = "🌐";
        }
    }
}
