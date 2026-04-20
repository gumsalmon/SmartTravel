using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using System.Net.Http.Json;
using HeriStep.Shared.Models;
using Microsoft.Maui.Networking;
using System.Collections.Generic;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Diagnostics;

namespace HeriStep.Client.Services
{
    public class AudioTranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalDatabaseService _localDb;
        private CancellationTokenSource? _cts;
        private IEnumerable<Locale>? _cachedLocales;
        private static bool _isWarmedUp = false;

        private static readonly Dictionary<string, string> LocaleMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = "VN",
            ["en"] = "US",
            ["ja"] = "JP",
            ["ja-jp"] = "JP",
            ["ko"] = "KR",
            ["ko-kr"] = "KR",
            ["zh"] = "CN",
            ["zh-hans"] = "CN",
            ["fr"] = "FR", ["fr-fr"] = "FR", ["fr-ca"] = "CA",
            ["es"] = "ES", ["es-es"] = "ES", ["es-mx"] = "MX",
            ["ru"] = "RU", ["ru-ru"] = "RU",
            ["th"] = "TH", ["th-th"] = "TH",
            ["de"] = "DE", ["de-de"] = "DE", ["de-at"] = "AT"
        };

        public AudioTranslationService(HttpClient httpClient, LocalDatabaseService localDb)
        {
            _httpClient = httpClient;
            _localDb = localDb;
            _httpClient.BaseAddress ??= new Uri($"{AppConstants.BaseApiUrl}/");
        }

        public async Task<string?> GetStallScriptAsync(int stallId, string? langCode = null)
        {
            var targetLang = string.IsNullOrWhiteSpace(langCode) ? L.CurrentLanguage : langCode.Trim().ToLowerInvariant();

            // 1. Kiểm tra kết nối mạng trước
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                var lastSynced = L.LastSyncedAudioLanguage;
                if (NormalizeLanguageCode(targetLang) != NormalizeLanguageCode(lastSynced))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var toastText = $"Mất kết nối. Đang sử dụng âm thanh {lastSynced.ToUpper()}...";
                        await Toast.Make(toastText, ToastDuration.Long).Show();
                    });
                }
                
                Console.WriteLine($"[VOICE_SERVICE] Device is offline. Fetching script for stall {stallId} from Local DB.");
                return await GetLocalfallbackScriptAsync(stallId, targetLang);
            }

            // 2. Nếu có mạng, thử gọi API Server
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StallSpeechResponse>($"api/Stalls/{stallId}/tts/{targetLang}");
                if (!string.IsNullOrWhiteSpace(response?.Text))
                {
                    return response.Text;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VOICE_SERVICE] API fetch failed: {ex.Message}. Falling back to Local DB.");
            }

            // 3. Fallback cuối cùng nếu API lỗi hoặc không có text
            return await GetLocalfallbackScriptAsync(stallId, targetLang);
        }

        private async Task<string?> GetLocalfallbackScriptAsync(int stallId, string targetLang)
        {
            try
            {
                var stall = await _localDb.GetStallByIdAsync(stallId);
                
                // 💡 CHỈ trả về TtsScript nếu ngôn ngữ đích là tiếng Việt (vì text trong DB là tiếng Việt)
                // Nếu là ngôn ngữ khác, trả về null để lớp trên dùng câu chào mặc định.
                if (NormalizeLanguageCode(targetLang) == "vi")
                {
                    return stall?.TtsScript;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task WarmUpAsync()
        {
            if (_isWarmedUp) return;
            
            try
            {
                // 💡 CHỐT: Lấy danh sách giọng đọc trên MainThread là cách an toàn nhất
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                    // Đã gỡ bỏ: await TextToSpeech.Default.SpeakAsync(" ", new SpeechOptions { Volume = 0 });
                    // Lưu ý: Việc ráng ép phát TTS bằng volume 0 làm engine Android tạo tiếng rè / click loa trong lần gọi đầu.
                });
                
                _isWarmedUp = true;
                Console.WriteLine("[VOICE_SERVICE] Engine warmed up on MainThread.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VOICE_SERVICE] Warm-up failed: {ex.Message}");
            }
        }

        private bool _isFirstSpeak = true;

        public async Task SpeakAsync(string scriptText, string? langCode = null)
        {
            if (string.IsNullOrWhiteSpace(scriptText)) return;

            // 💡 Ensure we have locales before speaking
            if (_cachedLocales == null)
            {
                try
                {
                    _cachedLocales = await MainThread.InvokeOnMainThreadAsync(async () => 
                        await TextToSpeech.Default.GetLocalesAsync());
                }
                catch { /* fallback to default */ }
            }

            if (_isFirstSpeak)
            {
                await Task.Delay(100);
                _isFirstSpeak = false;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // 💡 CHỐT: Chỉ chạy trên MainThread để đảm bảo tính ổn định của TTS Engine
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    string targetLang = NormalizeLanguageCode(string.IsNullOrWhiteSpace(langCode) ? L.CurrentLanguage : langCode);
                    
                    if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    {
                        targetLang = NormalizeLanguageCode(L.LastSyncedAudioLanguage);
                    }
                    
                    IEnumerable<Locale>? locales = _cachedLocales;
                    var preferredCountry = LocaleMap.TryGetValue(targetLang, out var mappedCountry) ? mappedCountry : string.Empty;
                    var languagePrefix = targetLang.Split('-')[0];

                    var locale = locales?
                        .Where(l =>
                            l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase) ||
                            l.Language.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(l => l.Country.Equals(preferredCountry, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    var options = new SpeechOptions
                    {
                        Locale = locale,
                        Pitch = 1.0f,
                        Volume = 1.0f // Sử dụng volume phần mềm 100% thay vì ép cứng volume hệ thống
                    };

                    Console.WriteLine($"[VOICE_SERVICE] Chuẩn bị phát: '{scriptText}' ({targetLang})");

#if ANDROID
                    // Vô hiệu hóa việc can thiệp vào AudioManager. Việc ép phần cứng Audio thiết lập lại .Mode = Normal 
                    // trước khi phát TTS có thể gây ra hiện tượng pop / rè loa ở một số máy Android.
#endif

                    try
                    {
                        // 🚀 Phát âm thanh (Sử dụng lệnh await trực tiếp, không dùng Timeout thủ công để tránh phát lặp)
                        await TextToSpeech.Default.SpeakAsync(scriptText, options, token);
                        Debug.WriteLine($"[VOICE_SERVICE] SpeakAsync hoàn tất: {targetLang}");
                    }
                    catch (Exception speakEx)
                    {
                        if (token.IsCancellationRequested) return;

                        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Toast.Make("Vui lòng tải gói ngôn ngữ hoặc bật mạng để nghe audio").Show();
                            });
                            return; // TUYỆT ĐỐI không tự động nhảy về đọc tiếng Việt
                        }

                        Debug.WriteLine($"[VOICE_SERVICE] Lỗi Locale cụ thể: {speakEx.Message}. Thử giọng mặc định...");
                        
                        // 🔄 FALLBACK: Chỉ gọi khi lệnh trên thực sự thất bại hoàn toàn
                        await TextToSpeech.Default.SpeakAsync(scriptText, new SpeechOptions { Volume = 1.0f }, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[VOICE_SERVICE] Speech đã bị hủy bởi người dùng.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VOICE_SERVICE] Lỗi nghiêm trọng: {ex.Message}");
                }
            });
        }

        private static string NormalizeLanguageCode(string? langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode)) return "vi";
            return langCode.Trim().ToLowerInvariant();
        }

        private sealed class StallSpeechResponse
        {
            public string Text { get; set; } = string.Empty;
            public string LangCode { get; set; } = string.Empty;
        }
    }
}
