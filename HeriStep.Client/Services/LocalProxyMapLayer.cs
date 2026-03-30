using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// Tạo tile layer trỏ vào Proxy Server của chúng ta (10.0.2.2:5297)
    /// thay vì trỏ thẳng lên OpenStreetMap → Giải quyết triệt để lỗi Trắng Map trên Android Emulator
    /// </summary>
    public static class LocalProxyMapLayer
    {
        private const string ProxyBaseUrl = "http://10.0.2.2:5297/api/map-tile";

        public static TileLayer Create()
        {
            // BruTile 6 dùng cú pháp khác - không có named parameter "userAgent"
            var tileSource = new HttpTileSource(
                new GlobalSphericalMercator(),
                $"{ProxyBaseUrl}/{{z}}/{{x}}/{{y}}",
                name: "HeriStep-ProxyOSM",
                attribution: new BruTile.Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright")
            );

            return new TileLayer(tileSource) { Name = "BaseMapLayer" };
        }
    }
}
