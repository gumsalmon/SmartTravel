using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

                // Bóc tách JSON
                using var doc = JsonDocument.Parse(response);
                var jsonArray = doc.RootElement[0];

                // 💡 FIX 1: Nối tất cả các câu lại với nhau (Google sẽ chia nhỏ đoạn văn thành nhiều mảng)
                StringBuilder fullTranslation = new StringBuilder();
                foreach (var item in jsonArray.EnumerateArray())
                {
                    fullTranslation.Append(item[0].GetString());
                }

                return fullTranslation.ToString();
            }
            catch
            {
                // 💡 FIX 2: BẮT BUỘC trả về chuỗi rỗng ("") để TranslationWorker biết là dịch lỗi.
                // Tuyệt đối không trả về 'text' ở đây, tránh tình trạng lưu tiếng Việt vào cột tiếng Hàn.
                return "";
            }
        }
    }
}