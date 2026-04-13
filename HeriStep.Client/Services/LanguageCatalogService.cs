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

        return new List<Language>
        {
            new() { LangCode = "vi", LangName = "Vietnamese" },
            new() { LangCode = "en", LangName = "English" },
            new() { LangCode = "ja", LangName = "Japanese" },
            new() { LangCode = "ko", LangName = "Korean" },
            new() { LangCode = "zh", LangName = "Chinese" },
            new() { LangCode = "fr", LangName = "French" },
            new() { LangCode = "es", LangName = "Spanish" },
            new() { LangCode = "ru", LangName = "Russian" },
            new() { LangCode = "th", LangName = "Thai" },
            new() { LangCode = "de", LangName = "German" }
        };
    }
}
