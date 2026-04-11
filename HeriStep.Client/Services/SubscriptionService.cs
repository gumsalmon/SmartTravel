using System.Net.Http.Json;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Services
{
    public class SubscriptionStatusResponse
    {
        public bool Valid { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public double? RemainingHours { get; set; }
    }

    public class PurchaseResponse
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public string? QrUrl { get; set; }
    }

    public class PaymentStatusResponse
    {
        public bool IsPaid { get; set; }
    }

    public class SubscriptionService
    {
        private readonly HttpClient _http;
        private const string CacheKey = "device_uuid";

        public SubscriptionService()
        {
            _http = new HttpClient 
            { 
                BaseAddress = new Uri(AppConstants.BaseApiUrl),
                Timeout = TimeSpan.FromSeconds(5) 
            };
        }

        public string GetDeviceId()
        {
            var deviceId = Preferences.Default.Get(CacheKey, string.Empty);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                Preferences.Default.Set(CacheKey, deviceId);
            }
            return deviceId;
        }

        public async Task<SubscriptionStatusResponse?> CheckStatusAsync()
        {
            try
            {
                var deviceId = GetDeviceId();
                var status = await _http.GetFromJsonAsync<SubscriptionStatusResponse>($"/api/Tickets/status/{deviceId}");
                if (status != null && status.Valid)
                {
                    // Cache the offline validity
                    Preferences.Default.Set("sub_expires_at", status.ExpiresAt?.ToString("o") ?? "");
                }
                return status;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error CheckStatusAsync: {ex.Message}");
                
                // OFFLINE FALLBACK
                var offlineExpiryStr = Preferences.Default.Get("sub_expires_at", "");
                if (DateTime.TryParse(offlineExpiryStr, out DateTime expiresAt))
                {
                    if (expiresAt > DateTime.UtcNow)
                    {
                        Console.WriteLine("[OFFLINE_DB] Using cached subscription validity.");
                        return new SubscriptionStatusResponse { Valid = true, ExpiresAt = expiresAt };
                    }
                }
                
                return new SubscriptionStatusResponse { Valid = false };
            }
        }

        public async Task<List<TicketPackage>> GetPackagesAsync()
        {
            try
            {
                var pkgs = await _http.GetFromJsonAsync<List<TicketPackage>>("/api/Tickets/packages");
                return pkgs ?? new List<TicketPackage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error GetPackagesAsync: {ex.Message}");
                return new List<TicketPackage>();
            }
        }

        public async Task<PurchaseResponse?> PurchasePackageAsync(int packageId)
        {
            try
            {
                var req = new
                {
                    DeviceId = GetDeviceId(),
                    PackageId = packageId
                };
                var response = await _http.PostAsJsonAsync("/api/Tickets/purchase", req);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error PurchasePackageAsync: {ex.Message}");
            }
            return new PurchaseResponse { Success = false };
        }

        public async Task<bool> CheckPaymentStatusAsync(string orderId)
        {
            try
            {
                var status = await _http.GetFromJsonAsync<PaymentStatusResponse>($"/api/Payments/check-status/{orderId}");
                return status?.IsPaid ?? false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
