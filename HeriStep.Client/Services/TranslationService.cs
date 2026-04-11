using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeriStep.Client.Services
{
    public static class TranslationService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// Translates text using the free Google Translate API endpoint.
        /// </summary>
        /// <param name="text">The source text to translate.</param>
        /// <param name="targetLanguage">The target language code (e.g., 'en', 'vi', 'ja').</param>
        /// <returns>The translated text, or the original text if translation fails.</returns>
        public static async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await _httpClient.GetStringAsync(url);
                
                // The response is a nested JSON array like: [[["Translated text","Original text",null,null,10]],null,"en", ...]
                // We parse it using System.Text.Json to extract the first element of the innermost array.
                using var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstElement = root[0];
                    if (firstElement.ValueKind == JsonValueKind.Array)
                    {
                        string translatedResult = "";
                        foreach (var chunk in firstElement.EnumerateArray())
                        {
                            if (chunk.ValueKind == JsonValueKind.Array && chunk.GetArrayLength() > 0)
                            {
                                translatedResult += chunk[0].GetString();
                            }
                        }
                        
                        return string.IsNullOrWhiteSpace(translatedResult) ? text : translatedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TranslationService] Error translating text: {ex.Message}");
            }

            return text; // Fallback to original text
        }
    }
}
