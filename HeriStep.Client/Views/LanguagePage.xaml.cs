using HeriStep.Client.Services;
using HeriStep.Shared.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace HeriStep.Client.Views
{
    public partial class LanguagePage : ContentPage
    {
        private readonly LanguageCatalogService _languageCatalog = new();
        private readonly bool _isChangeMode;
        private string? _selectedLang;
        public ObservableCollection<Language> Languages { get; } = new();

        public LanguagePage(bool isChangeMode = false)
        {
            InitializeComponent();
            _isChangeMode = isChangeMode;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Languages.Count == 0)
            {
                var items = await _languageCatalog.GetLanguagesAsync();
                Languages.Clear();
                foreach (var language in items)
                {
                    Languages.Add(language);
                }
            }

            if (_isChangeMode && !string.IsNullOrWhiteSpace(L.CurrentLanguage))
            {
                var current = Languages.FirstOrDefault(l => l.LangCode == L.CurrentLanguage);
                if (current != null)
                {
                    languageCollection.SelectedItem = current;
                    _selectedLang = current.LangCode;
                    btnGetStarted.IsEnabled = true;
                    btnGetStarted.Opacity = 1.0;
                }
            }
        }

        private void OnLanguageSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Language language && !string.IsNullOrWhiteSpace(language.LangCode))
            {
                _selectedLang = language.LangCode;
                btnGetStarted.IsEnabled = true;
                btnGetStarted.Opacity = 1.0;
            }
        }

        private async void OnGetStartedClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedLang)) return;

            btnGetStarted.IsEnabled = false;
            btnGetStarted.Text = "Loading...";

            try
            {
                // Save language + defaults
                L.SetLanguage(_selectedLang);
                Preferences.Default.Set("has_selected_language", true);
                Preferences.Default.Set("voice_speed", 1.0f);
                Preferences.Default.Set("voice_radius", 50.0);

                Console.WriteLine($"[LOG] Language changed to: {_selectedLang}");

                if (_isChangeMode)
                {
                    // Just go back — AppShell stays alive, L.LanguageChanged fires and refreshes everything
                    await Navigation.PopAsync();
                }
                else
                {
                    // First-time setup: navigate to main shell
                    if (Application.Current?.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Setup error: {ex.Message}", "OK");
                btnGetStarted.IsEnabled = true;
                btnGetStarted.Text = "GET STARTED";
            }
        }
    }
}
