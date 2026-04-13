using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using System.Net.Http.Json;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Services
{
    public class AudioTranslationService
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;

        private static readonly Dictionary<string, string> LocaleMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = "VN",
            ["en"] = "US",
            ["ja"] = "JP",
            ["ko"] = "KR",
            ["zh"] = "CN",
            ["fr"] = "FR",
            ["es"] = "ES",
            ["ru"] = "RU",
            ["th"] = "TH",
            ["de"] = "DE"
        };

        public AudioTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri($"{AppConstants.BaseApiUrl}/");
        }

        public async Task<string?> GetStallScriptAsync(int stallId, string? langCode = null)
        {
            var targetLang = string.IsNullOrWhiteSpace(langCode) ? L.CurrentLanguage : langCode.Trim().ToLowerInvariant();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StallSpeechResponse>($"api/Stalls/{stallId}/tts/{targetLang}");
                if (!string.IsNullOrWhiteSpace(response?.Text))
                {
                    return response.Text;
                }
            }
            catch
            {
                // ignore network errors and use caller fallback
            }

            return null;
        }

        public async Task SpeakAsync(string scriptText, string? langCode = null)
        {
            if (string.IsNullOrWhiteSpace(scriptText)) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                string targetLang = string.IsNullOrWhiteSpace(langCode) ? L.CurrentLanguage : langCode.Trim().ToLowerInvariant();
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var preferredCountry = LocaleMap.TryGetValue(targetLang, out var mappedCountry) ? mappedCountry : string.Empty;

                var locale = locales?
                    .Where(l => l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(l => l.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                var options = new SpeechOptions
                {
                    Locale = locale,
                    Pitch = 1.0f,
                    Volume = 1.0f
                };

#if ANDROID
                // 🔊 Ép tăng âm lượng media lên tối đa để nghe trên loa ngoài không cần tai nghe
                try
                {
                    var audioManager = (Android.Media.AudioManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.AudioService);
                    if (audioManager != null)
                    {
                        int maxVolume = audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
                        audioManager.SetStreamVolume(Android.Media.Stream.Music, maxVolume, Android.Media.VolumeNotificationFlags.RemoveSoundAndVibrate);
                        Console.WriteLine($"[VOICE_SERVICE] Android volume set to max ({maxVolume})");
                    }
                }
                catch (Exception volEx)
                {
                    Console.WriteLine($"[VOICE_SERVICE] Volume set failed: {volEx.Message}");
                }
#endif

                await TextToSpeech.Default.SpeakAsync(scriptText, options, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[VOICE_SERVICE] Speech cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VOICE_SERVICE] Error: {ex.Message}");
            }
        }

        private sealed class StallSpeechResponse
        {
            public string Text { get; set; } = string.Empty;
            public string LangCode { get; set; } = string.Empty;
        }
    }
}
