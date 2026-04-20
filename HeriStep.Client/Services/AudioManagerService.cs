using System;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Audio;
using Microsoft.Maui.Media;

namespace HeriStep.Client.Services
{
    public class AudioManagerService
    {
        private readonly IAudioManager _audioManager;
        private readonly AudioTranslationService _ttsService;
        private IAudioPlayer? _currentPlayer;
        private CancellationTokenSource? _cts;

        public AudioManagerService(IAudioManager audioManager, AudioTranslationService ttsService)
        {
            _audioManager = audioManager;
            _ttsService = ttsService;
        }

        public bool IsPlaying => (_currentPlayer?.IsPlaying ?? false) || (_cts != null && !_cts.IsCancellationRequested);

        public async Task PlayStallAudioAsync(int stallId, string? audioUrl, string textScriptFallback)
        {
            StopAll();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                if (!string.IsNullOrWhiteSpace(audioUrl))
                {
                    // 1. Nếu có file ghi âm sẵn -> Ưu tiên phát
                    Console.WriteLine($"[AUDIO_MANAGER] Đang phát file âm thanh cho điểm: {stallId}");
                    
                    var stream = await GetAudioStreamAsync(audioUrl, token);
                    if (stream != null)
                    {
                        _currentPlayer = _audioManager.CreatePlayer(stream);
                        _currentPlayer.Play();
                        return;
                    }
                }

                // 2. Fallback về Text To Speech nếu không có AudioUrl hoặc lỗi tải
                Console.WriteLine($"[AUDIO_MANAGER] Fallback đọc TTS cho điểm: {stallId}");
                await _ttsService.SpeakAsync(textScriptFallback);
            }
            catch (OperationCanceledException)
            {
                // Ngắt ngang khi có điểm ưu tiên khác chen vào
                Console.WriteLine($"[AUDIO_MANAGER] Đã hủy chuỗi âm thanh trước đó.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIO_MANAGER] Lỗi phát âm thanh: {ex.Message}");
            }
        }

        public void StopAll()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_currentPlayer != null)
                {
                    if (_currentPlayer.IsPlaying)
                    {
                        _currentPlayer.Stop();
                    }
                    _currentPlayer.Dispose();
                    _currentPlayer = null;
                }
            }
            catch { }
        }

        private async Task<Stream?> GetAudioStreamAsync(string url, CancellationToken token)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await client.GetAsync(url, token);
                if (response.IsSuccessStatusCode)
                {
                    // Copy vào MemoryStream vì IAudioPlayer cần một Stream có khả năng Seek tùy nền tảng
                    var memStream = new MemoryStream();
                    await response.Content.CopyToAsync(memStream, token);
                    memStream.Position = 0;
                    return memStream;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIO_MANAGER] Lỗi tải File Âm Thanh mạng: {ex.Message}");
            }
            return null;
        }
    }
}
