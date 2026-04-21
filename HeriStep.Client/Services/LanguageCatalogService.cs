using System.Net.Http.Json;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Services;

public class LanguageCatalogService
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri($"{AppConstants.BaseApiUrl}/")
    };

    public async Task<List<Language>> GetLanguagesAsync()
    {
        try
        {
            var languages = await _httpClient.GetFromJsonAsync<List<Language>>("api/Stalls/languages");
            if (languages is { Count: > 0 })
            {
                // 💡 Tự động nhận diện và sửa lỗi Encoding nếu dữ liệu từ API bị lỗi (chứa Ã, Â, º...)
                foreach (var l in languages)
                {
                    if (IsCorrupted(l.LangName))
                    {
                         l.LangName = GetFallbackName(l.LangCode);
                    }
                }

                return languages
                    .Where(l => !string.IsNullOrWhiteSpace(l.LangCode))
                    .OrderBy(l => l.LangName)
                    .ToList();
            }
        }
        catch
        {
            // fall back to embedded set when API is unavailable
        }

        return GetOfflineLanguages();
    }

    private bool IsCorrupted(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        // Các ký tự đặc trưng của lỗi UTF-8 as Win-1252 (Mở rộng thêm cho tiếng Trung/Đức/Nga bị lỗi)
        string[] badCharacters = { "Ã", "Â", "º", "Æ", "â", "ä", "æ", "»", "Ä", "", "Ñ", "Ð" };
        return badCharacters.Any(c => text.Contains(c));
    }

    private string GetFallbackName(string langCode)
    {
        return langCode.ToLower() switch
        {
            "vi" => "Tiếng Việt",
            "en" => "English",
            "fr" => "Français",
            "es" => "Español",
            "ja" => "日本語",
            "ko" => "한국어",
            "zh" => "中文",
            "th" => "ภาษาไทย",
            "ru" => "Русский",
            "de" => "Deutsch",
            _ => langCode.ToUpper()
        };
    }

    private List<Language> GetOfflineLanguages()
    {
        return new List<Language>
        {
            new() { LangCode = "vi", LangName = "Tiếng Việt" },
            new() { LangCode = "en", LangName = "English" },
            new() { LangCode = "ja", LangName = "日本語" },
            new() { LangCode = "ko", LangName = "한국어" },
            new() { LangCode = "zh", LangName = "中文" },
            new() { LangCode = "fr", LangName = "Français" },
            new() { LangCode = "es", LangName = "Español" },
            new() { LangCode = "ru", LangName = "Русский" },
            new() { LangCode = "th", LangName = "ภาษาไทย" },
            new() { LangCode = "de", LangName = "Deutsch" }
        };
    }
}
