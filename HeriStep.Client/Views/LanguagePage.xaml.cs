using HeriStep.Client.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HeriStep.Client.Views
{
    public partial class LanguagePage : ContentPage
    {
        // Savory Ember colors
        private static readonly Color GlassNormal  = Color.FromArgb("#CC2A2A2A");
        private static readonly Color GlassSelected = Color.FromArgb("#CC3D1A00");
        private static readonly Color StrokeNormal  = Color.FromArgb("#40FFFFFF");
        private static readonly Color StrokeSelected = Color.FromArgb("#D35400");

        private string? _selectedLang;

        // Map: language code → card Border
        private Dictionary<string, Border> _cardMap = new();

        public LanguagePage()
        {
            InitializeComponent();

            // Build the code → card mapping after InitializeComponent
            _cardMap = new Dictionary<string, Border>
            {
                ["vi"] = cardVI,
                ["en"] = cardEN,
                ["ja"] = cardJA,
                ["ko"] = cardKO,
                ["fr"] = cardFR,
                ["de"] = cardDE,
                ["zh"] = cardZH,
                ["es"] = cardES,
            };
        }

        // ════════════════════════════════════════════
        // CARD TAP HANDLER — highlight selected card
        // ════════════════════════════════════════════

        private void OnCardTapped(object sender, TappedEventArgs e)
        {
            var lang = e.Parameter as string;
            if (string.IsNullOrEmpty(lang)) return;

            _selectedLang = lang;

            // Reset all cards to unselected style
            foreach (var kvp in _cardMap)
            {
                kvp.Value.BackgroundColor = GlassNormal;
                kvp.Value.Stroke = StrokeNormal;
                kvp.Value.StrokeThickness = 1;
            }

            // Apply selected style to tapped card
            if (_cardMap.TryGetValue(lang, out var card))
            {
                card.BackgroundColor = GlassSelected;   // warm ember tint
                card.Stroke = StrokeSelected;            // amber border
                card.StrokeThickness = 2;
            }

            // Enable the GET STARTED button
            btnGetStarted.IsEnabled = true;
            btnGetStarted.Opacity = 1.0;
        }

        // ════════════════════════════════════════════
        // GET STARTED — persist & navigate to shell
        // ════════════════════════════════════════════

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

                // Navigate to the main app shell
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
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
