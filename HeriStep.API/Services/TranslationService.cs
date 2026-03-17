using System.Text.Json;

namespace HeriStep.API.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;

        public TranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Hàm dịch tự động
        public async Task<string> TranslateTextAsync(string text, string targetLangCode, string sourceLangCode = "vi")
        {
            // Nếu text rỗng hoặc ngôn ngữ đích giống ngôn ngữ gốc thì trả về y nguyên
            if (string.IsNullOrWhiteSpace(text) || targetLangCode == sourceLangCode)
                return text;

            try
            {
                // Gọi API Google Translate miễn phí (Endpoint gtx)
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLangCode}&tl={targetLangCode}&dt=t&q={Uri.EscapeDataString(text)}";

                var response = await _httpClient.GetStringAsync(url);

                // Bóc tách chuỗi JSON loằng ngoằng của Google để lấy đúng câu đã dịch
                using var doc = JsonDocument.Parse(response);
                var translatedText = doc.RootElement[0][0][0].GetString();

                return translatedText ?? text;
            }
            catch
            {
                // Nếu rớt mạng hoặc Google chặn, trả về câu gốc để không làm sập Server
                return text;
            }
        }
    }
}