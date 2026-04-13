using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Devices;
using HeriStep.Client.Services;

namespace HeriStep.Client.Services
{
    public class AudioTranslationService
    {
        private bool _isBusy = false;

        public async Task SpeakAsync(string originalText)
        {
            if (_isBusy) return;
            if (string.IsNullOrWhiteSpace(originalText)) return;

            _isBusy = true;
            try
            {
                string targetLang = L.CurrentLanguage;
                Console.WriteLine($"[VOICE_SERVICE] Target Language: {targetLang}");

                // 1. Auto-Translate
                Console.WriteLine($"[VOICE_SERVICE] Translating text: {originalText.Substring(0, Math.Min(20, originalText.Length))}...");
                string translatedText = await TranslationService.TranslateTextAsync(originalText, targetLang);
                Console.WriteLine($"[VOICE_SERVICE] Translation complete: {translatedText.Substring(0, Math.Min(20, translatedText.Length))}...");

                // 2. Setup TTS Options (Hardcoded Female Preference & National Locale)
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                
                // Mở rộng Locale mapping chuẩn (ưu tiên người bản xứ)
                var nativeMappings = new Dictionary<string, string>
                {
                    { "zh", "CN" }, { "ko", "KR" }, { "ja", "JP" },
                    { "vi", "VN" }, { "en", "US" }, { "fr", "FR" }
                };

                string preferredCountry = nativeMappings.ContainsKey(targetLang) ? nativeMappings[targetLang] : "";

                // Find locale matching target language - ưu tiên quốc gia gốc
                var locale = locales?
                    .Where(l => l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(l => l.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(l => l.Country)
                    .FirstOrDefault();

                if (locale != null)
                {
                    Console.WriteLine($"[VOICE_SERVICE] NATIVE DETECTED: Found matching voice for {targetLang.ToUpper()} -> {locale.Name} ({locale.Country})");
                }
                else
                {
                    Console.WriteLine($"[VOICE_SERVICE] WARNING: No native voice found for {targetLang.ToUpper()}, using system default.");
                }

                var options = new SpeechOptions
                {
                    Locale = locale,
                    Pitch = 1.2f, // Female preference
                    Volume = 1.0f
                };

                // 3. Speak
                string nationalityLabel = targetLang switch {
                    "vi" => "người Việt Nam",
                    "en" => "người Mỹ/Anh",
                    "ja" => "người Nhật Bản",
                    "ko" => "người Hàn Quốc",
                    "fr" => "người Pháp",
                    "zh" => "người Trung Quốc",
                    _ => "bản ngữ"
                };
                Console.WriteLine($"[VOICE_SERVICE] ACTION: Đang phát âm thanh bằng giọng của {nationalityLabel}...");
                await TextToSpeech.Default.SpeakAsync(translatedText, options);
                Console.WriteLine($"[VOICE_SERVICE] Speech completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VOICE_SERVICE] Error: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
