using HeriStep.Shared.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public DashboardStats Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate   { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = $"?startDate={StartDate?.ToString("yyyy-MM-dd")}&endDate={EndDate?.ToString("yyyy-MM-dd")}";
                Stats = await _http.GetFromJsonAsync<DashboardStats>($"api/Stats{query}") ?? new DashboardStats();
            }
            catch
            {
                Stats = new DashboardStats();
            }
        }
    }
}