using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Devices;
using HeriStep.Client.Services;

namespace HeriStep.Client.Services
{
    public class AudioTranslationService
    {
        // Dùng CancellationToken để huỷ TTS đang phát trước khi bắt đầu cái mới
        private CancellationTokenSource? _cts;

        public async Task SpeakAsync(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText)) return;

            // Huỷ giọng đọc đang chạy (nếu có) để tránh rè/xếp hàng
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                string targetLang = L.CurrentLanguage;
                Console.WriteLine($"[VOICE_SERVICE] Target Language: {targetLang}");

                // 1. Auto-Translate
                string translatedText = await TranslationService.TranslateTextAsync(originalText, targetLang);
                Console.WriteLine($"[VOICE_SERVICE] Translation: {translatedText.Substring(0, Math.Min(30, translatedText.Length))}...");

                // Nếu bị huỷ trong lúc dịch thì bỏ qua
                if (token.IsCancellationRequested) return;

                // 2. Setup TTS Options
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                var nativeMappings = new Dictionary<string, string>
                {
                    { "zh", "CN" }, { "ko", "KR" }, { "ja", "JP" },
                    { "vi", "VN" }, { "en", "US" }, { "fr", "FR" }
                };

                string preferredCountry = nativeMappings.ContainsKey(targetLang) ? nativeMappings[targetLang] : "";

                var locale = locales?
                    .Where(l => l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(l => l.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(l => l.Country)
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

                // 3. Speak với CancellationToken
                Console.WriteLine($"[VOICE_SERVICE] Speaking...");
                await TextToSpeech.Default.SpeakAsync(translatedText, options, token);
                Console.WriteLine($"[VOICE_SERVICE] Speech completed.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[VOICE_SERVICE] Speech cancelled (new request came in).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VOICE_SERVICE] Error: {ex.Message}");
            }
        }
    }
}
